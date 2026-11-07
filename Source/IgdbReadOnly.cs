using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SmartMetadataMatcher
{
    [DataContract]
    internal sealed class IgdbAuth
    {
        [DataMember(Name="access_token")] public string Token { get; set; }
    }
    [DataContract]
    internal sealed class IgdbPlatform
    {
        [DataMember(Name="id")] public long Id { get; set; }
        [DataMember(Name="name")] public string Name { get; set; }
    }
    [DataContract]
    internal sealed class IgdbAlias
    {
        [DataMember(Name="name")] public string Name { get; set; }
    }
    [DataContract]
    internal sealed class IgdbMedia
    {
        [DataMember(Name="image_id")] public string ImageId { get; set; }
    }
    [DataContract]
    internal sealed class IgdbNamed
    {
        [DataMember(Name="name")] public string Name { get; set; }
    }
    [DataContract]
    internal sealed class IgdbCompanyRelation
    {
        [DataMember(Name="developer")] public bool Developer { get; set; }
        [DataMember(Name="publisher")] public bool Publisher { get; set; }
        [DataMember(Name="company")] public IgdbNamed Company { get; set; }
    }
    [DataContract]
    internal sealed class IgdbRelease
    {
        [DataMember(Name="date")] public long? Date { get; set; }
        [DataMember(Name="platform")] public IgdbPlatform Platform { get; set; }
    }
    [DataContract]
    internal sealed class IgdbRecord
    {
        [DataMember(Name="id")] public long Id { get; set; }
        [DataMember(Name="name")] public string Name { get; set; }
        [DataMember(Name="first_release_date")] public long? FirstReleaseDate { get; set; }
        [DataMember(Name="summary")] public string Summary { get; set; }
        [DataMember(Name="platforms")] public List<IgdbPlatform> Platforms { get; set; }
        [DataMember(Name="alternative_names")] public List<IgdbAlias> Aliases { get; set; }
        [DataMember(Name="cover")] public IgdbMedia Cover { get; set; }
        [DataMember(Name="screenshots")] public List<IgdbMedia> Screenshots { get; set; }
        [DataMember(Name="genres")] public List<IgdbNamed> Genres { get; set; }
        [DataMember(Name="involved_companies")] public List<IgdbCompanyRelation> InvolvedCompanies { get; set; }
        [DataMember(Name="collections")] public List<IgdbNamed> Collections { get; set; }
        [DataMember(Name="release_dates")] public List<IgdbRelease> ReleaseDates { get; set; }
        [DataMember(Name="game_type")] public long? GameTypeId { get; set; }
        [DataMember(Name="version_parent")] public long? VersionParentId { get; set; }
        [DataMember(Name="parent_game")] public long? ParentGameId { get; set; }
        [DataMember(Name="version_title")] public string VersionTitle { get; set; }
        [DataMember(Name="total_rating_count")] public int? TotalRatingCount { get; set; }
    }
    [DataContract]
    internal sealed class CatalogCredentialData
    {
        [DataMember] public string ClientId { get; set; }
        [DataMember] public string ClientSecret { get; set; }
    }

    internal sealed class CatalogCredentialStore
    {
        private readonly string path;
        internal CatalogCredentialStore(string pluginDataDir)
        {
            path = Path.Combine(pluginDataDir, "igdb-oauth-user.dpapi");
        }
        internal CatalogCredentialData Load()
        {
            try
            {
                if (!File.Exists(path)) return new CatalogCredentialData();
                var encrypted = File.ReadAllBytes(path);
                var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                using (var stream = new MemoryStream(plain))
                    return (CatalogCredentialData)new DataContractJsonSerializer(typeof(CatalogCredentialData)).ReadObject(stream);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Saved IGDB credentials are inaccessible for this Windows account. Re-enter and save them.");
            }
        }
        internal void Save(string clientId, string clientSecret)
        {
            if (String.IsNullOrWhiteSpace(clientId) || String.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("Client ID and client secret are both required.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var record = new CatalogCredentialData { ClientId=clientId.Trim(), ClientSecret=clientSecret.Trim() };
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(CatalogCredentialData)).WriteObject(stream, record);
                bytes = stream.ToArray();
            }
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, encrypted);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }
    }

    // Only sanitized outcomes are returned to the WPF dialog; never propagate an
    // arbitrary HttpClient exception or provider response body to the UI.
    internal sealed class IgdbConnectionResult
    {
        internal bool Connected { get; private set; }
        internal string Message { get; private set; }
        private IgdbConnectionResult(bool connected,string message)
        { Connected=connected;Message=message; }
        internal static IgdbConnectionResult Success()
        { return new IgdbConnectionResult(true,"Connected to IGDB. Twitch credentials and read-only game lookup are working."); }
        internal static IgdbConnectionResult BadResponse()
        { return new IgdbConnectionResult(false,"The service returned an unexpected response. Try again later."); }
        internal static IgdbConnectionResult Network()
        { return new IgdbConnectionResult(false,"Could not contact Twitch/IGDB. Check your internet connection and firewall, then retry."); }
        internal static IgdbConnectionResult Timeout()
        { return new IgdbConnectionResult(false,"The connection timed out. Check network access and try again."); }
        internal static IgdbConnectionResult FromHttp(int status,bool oauth)
        {
            if(oauth && (status==400 || status==401 || status==403))
                return new IgdbConnectionResult(false,"Twitch rejected the Client ID or Client Secret. Verify both in the Twitch Developer Console.");
            if(!oauth && (status==401 || status==403))
                return new IgdbConnectionResult(false,"Twitch accepted the credentials, but IGDB denied game lookup (HTTP "+status+"). Check the application's IGDB access.");
            if(status==429)
                return new IgdbConnectionResult(false,"Twitch/IGDB is rate-limiting requests. Try again later.");
            if(status>=500)
                return new IgdbConnectionResult(false,"Twitch/IGDB is temporarily unavailable (HTTP "+status+"). Try again later.");
            return new IgdbConnectionResult(false,"Twitch/IGDB returned HTTP "+status+" during "+(oauth?"authentication":"game lookup")+". Check settings or try again later.");
        }
    }

    internal sealed class IgdbAuthenticationException : InvalidOperationException
    {
        internal int HttpStatus { get; private set; }
        internal IgdbAuthenticationException(int status)
            : base("Twitch authentication failed (HTTP "+status+"). Verify local credentials and app configuration.")
        { HttpStatus=status; }
    }

    // Fixed official endpoints only. Tokens stay in memory; secrets are never logged or exported.
    internal sealed class IgdbReadOnly : IDisposable
    {
        private readonly HttpClient http;
        private readonly string clientId;
        private readonly string secret;
        private string token;
        // In-memory cache for this scan batch only; never persists user library titles.
        private readonly Dictionary<string,List<CatalogGame>> cache =
            new Dictionary<string,List<CatalogGame>>(StringComparer.Ordinal);
        internal IgdbReadOnly(string id, string secretValue)
            : this(id,secretValue,new HttpClientHandler()) { }
        // Injected only for offline HTTP tests; production uses HttpClientHandler.
        internal IgdbReadOnly(string id,string secretValue,HttpMessageHandler handler)
        {
            clientId = (id ?? "").Trim();
            secret = (secretValue ?? "").Trim();
            if (clientId.Length == 0 || secret.Length == 0)
                throw new InvalidOperationException("Configure IGDB credentials first.");
            if(handler==null)throw new ArgumentNullException("handler");
            http = new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(25);
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
        private static T Parse<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
        }
        private void Authenticate(CancellationToken ct)
        {
            if (!String.IsNullOrEmpty(token)) return;
            var fields = new[] {
                new KeyValuePair<string,string>("client_id",clientId),
                new KeyValuePair<string,string>("client_secret",secret),
                new KeyValuePair<string,string>("grant_type","client_credentials")
            };
            using (var body = new FormUrlEncodedContent(fields))
            using (var response = http.PostAsync("https://id.twitch.tv/oauth2/token", body, ct).GetAwaiter().GetResult())
            {
                if (!response.IsSuccessStatusCode)
                    throw new IgdbAuthenticationException((int)response.StatusCode);
                var auth = Parse<IgdbAuth>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                if (auth == null || String.IsNullOrWhiteSpace(auth.Token))
                    throw new InvalidOperationException("Twitch returned no usable access token.");
                token = auth.Token;
            }
        }
        // Test a credential pair in memory. Never save it, log it or touch Playnite.
        // Do not equate OAuth success with permission to query the IGDB API.
        internal IgdbConnectionResult TestConnection(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                Authenticate(ct);
                ct.ThrowIfCancellationRequested();
                using(var message=new HttpRequestMessage(HttpMethod.Post,"https://api.igdb.com/v4/games"))
                {
                    message.Headers.Add("Client-ID",clientId);
                    message.Headers.Authorization=
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);
                    message.Content=new StringContent("fields id; limit 1;",Encoding.UTF8,"text/plain");
                    using(var response=http.SendAsync(message,ct).GetAwaiter().GetResult())
                    {
                        if(!response.IsSuccessStatusCode)
                            return IgdbConnectionResult.FromHttp((int)response.StatusCode,false);
                        var records=Parse<List<IgdbRecord>>(
                            response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                        if(records==null)return IgdbConnectionResult.BadResponse();
                        ct.ThrowIfCancellationRequested();
                        return IgdbConnectionResult.Success();
                    }
                }
            }
            catch(IgdbAuthenticationException ex)
            { return IgdbConnectionResult.FromHttp(ex.HttpStatus,true); }
            catch(OperationCanceledException)
            {
                if(ct.IsCancellationRequested)throw;
                return IgdbConnectionResult.Timeout();
            }
            catch(HttpRequestException)
            { return IgdbConnectionResult.Network(); }
            catch(WebException)
            { return IgdbConnectionResult.Network(); }
            catch(SerializationException)
            { return IgdbConnectionResult.BadResponse(); }
            catch(InvalidOperationException)
            { return IgdbConnectionResult.BadResponse(); }
            catch(IOException)
            { return IgdbConnectionResult.Network(); }
            catch(Exception)
            { return IgdbConnectionResult.BadResponse(); } // Never display raw provider/client exceptions.
        }

        private static int ReleaseYear(long? seconds)
        {
            if (!seconds.HasValue || seconds.Value <= 0) return 0;
            try { return new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(seconds.Value).Year; }
            catch (ArgumentOutOfRangeException) { return 0; }
        }
        // Use the known-good /games endpoint. Each bounded variant is a separate query;
        // the 300ms pacing keeps total request traffic below IGDB's 4 requests/second limit.
        // An empty result means zero verified results, never silent authentication failure.
        internal List<CatalogGame> SearchVariants(IEnumerable<string> titleVariants, CancellationToken ct)
        {
            var queries=(titleVariants ?? Enumerable.Empty<string>())
                .Select(CatalogCore.BuildSingleQueryBody)
                .Where(q=>!String.IsNullOrWhiteSpace(q))
                .Distinct(StringComparer.Ordinal)
                .Take(4).ToList();
            if (queries.Count==0) return new List<CatalogGame>();
            var key=String.Join("\n",queries);
            List<CatalogGame> cached;
            if (cache.TryGetValue(key,out cached)) return cached;
            Authenticate(ct);
            var combined=new Dictionary<long,CatalogGame>();
            foreach(var query in queries)
            {
                ct.ThrowIfCancellationRequested();
                for(int attempt=0;attempt<3;attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    using(var message=new HttpRequestMessage(HttpMethod.Post,"https://api.igdb.com/v4/games"))
                    {
                        message.Headers.Add("Client-ID",clientId);
                        message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);
                        message.Content=new StringContent(query,Encoding.UTF8,"text/plain");
                        using(var response=http.SendAsync(message,ct).GetAwaiter().GetResult())
                        {
                            int status=(int)response.StatusCode;
                            bool retry=status==429 || status==502 || status==503 || status==504;
                            if(retry && attempt<2)
                            {
                                if(ct.WaitHandle.WaitOne(800*(attempt+1))) ct.ThrowIfCancellationRequested();
                                continue;
                            }
                            if(!response.IsSuccessStatusCode)
                                throw new InvalidOperationException("IGDB /games returned HTTP "+status+". Scan stopped; no Playnite data changed.");
                            var json=response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            var received=Parse<List<IgdbRecord>>(json) ?? new List<IgdbRecord>();
                            foreach(var x in received.Where(x=>x!=null && x.Id>0))
                            {
                                if(combined.ContainsKey(x.Id)) continue;
                                combined.Add(x.Id,new CatalogGame {
                                    Id=x.Id,Name=x.Name,Summary=x.Summary,
                                    GameTypeId=x.GameTypeId,VersionParentId=x.VersionParentId,
                                    ParentGameId=x.ParentGameId,VersionTitle=x.VersionTitle,
                                    RatingCount=x.TotalRatingCount.GetValueOrDefault(),
                                    FirstReleaseYear=ReleaseYear(x.FirstReleaseDate),
                                    Platforms=(x.Platforms ?? new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Name).ToList(),
                                    PlatformIds=(x.Platforms ?? new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Id).ToList(),
                                    Aliases=(x.Aliases ?? new List<IgdbAlias>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
                                    ReleaseDates=(x.ReleaseDates??new List<IgdbRelease>()).Where(r=>r!=null)
                                        .Select(r=>new CatalogRelease{PlatformId=r.Platform==null?0:r.Platform.Id,Year=ReleaseYear(r.Date)}).ToList(),
                                    CoverImageId=x.Cover==null?"":x.Cover.ImageId,
                                    ScreenshotImageId=x.Screenshots==null || x.Screenshots.Count==0 || x.Screenshots[0]==null
                                        ? "":x.Screenshots[0].ImageId
                                });
                            }
                            break;
                        }
                    }
                }
                // Also pace queries from the same game; the UI already delays between games.
                if(ct.WaitHandle.WaitOne(310)) ct.ThrowIfCancellationRequested();
            }
            var result=combined.Values.ToList();
            cache[key]=result;
            return result;
        }
        // Read-only follow-up for a small set of competing IDs. Uses a fixed
        // query with numeric IDs only. No scan result can become write-eligible
        // through this method; the existing strict preflight remains unchanged.
        internal List<CatalogGame> InspectCandidateIds(IEnumerable<long> ids,CancellationToken ct)
        {
            var selected=(ids??Enumerable.Empty<long>()).Distinct().ToList();
            if(selected.Count==0)return new List<CatalogGame>();
            if(selected.Count>CatalogCandidateEvidence.MaxIds || selected.Any(id=>id<=0))
                throw new ArgumentException("Out-of-bounds IGDB candidate investigation");
            ct.ThrowIfCancellationRequested();
            var key="evidence:"+String.Join(",",selected.OrderBy(x=>x));
            List<CatalogGame> saved;
            if(cache.TryGetValue(key,out saved))return saved;
            Authenticate(ct);
            var query="fields id,name,platforms.id,platforms.name,game_type,version_parent,"+
                "parent_game,version_title,release_dates.date,release_dates.platform.id; where id = ("+
                String.Join(",",selected.Select(x=>x.ToString(System.Globalization.CultureInfo.InvariantCulture)))+
                "); limit "+CatalogCandidateEvidence.MaxIds+";";
            for(int attempt=0;attempt<3;attempt++)
            {
                ct.ThrowIfCancellationRequested();
                using(var message=new HttpRequestMessage(HttpMethod.Post,"https://api.igdb.com/v4/games"))
                {
                    message.Headers.Add("Client-ID",clientId);
                    message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);
                    message.Content=new StringContent(query,Encoding.UTF8,"text/plain");
                    using(var response=http.SendAsync(message,ct).GetAwaiter().GetResult())
                    {
                        int status=(int)response.StatusCode;
                        if((status==429 || status==502 || status==503 || status==504) && attempt<2)
                        {
                            if(ct.WaitHandle.WaitOne(800*(attempt+1)))ct.ThrowIfCancellationRequested();
                            continue;
                        }
                        if(!response.IsSuccessStatusCode)
                            throw new InvalidOperationException("IGDB read-only candidate probe failed (HTTP "+status+").");
                        var received=Parse<List<IgdbRecord>>(
                            response.Content.ReadAsStringAsync().GetAwaiter().GetResult()) ?? new List<IgdbRecord>();
                        var allow=new HashSet<long>(selected);
                        var result=received.Where(x=>x!=null && allow.Contains(x.Id))
                            .GroupBy(x=>x.Id).Select(g=>g.First()).Select(x=>new CatalogGame {
                                Id=x.Id,Name=x.Name,GameTypeId=x.GameTypeId,
                                VersionParentId=x.VersionParentId,ParentGameId=x.ParentGameId,
                                VersionTitle=x.VersionTitle,RatingCount=x.TotalRatingCount.GetValueOrDefault(),
                                PlatformIds=(x.Platforms??new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Id).ToList(),
                                Platforms=(x.Platforms??new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Name).ToList(),
                                ReleaseDates=(x.ReleaseDates??new List<IgdbRelease>()).Where(r=>r!=null)
                                    .Select(r=>new CatalogRelease { PlatformId=r.Platform==null?0:r.Platform.Id,
                                        Year=ReleaseYear(r.Date) }).ToList()
                            }).ToList();
                        cache[key]=result;
                        if(ct.WaitHandle.WaitOne(310))ct.ThrowIfCancellationRequested();
                        return result;
                    }
                }
            }
            throw new InvalidOperationException("IGDB read-only candidate probe failed.");
        }

        // Read-only exact-name discovery fallback. Unlike IGDB's broad search,
        // case-insensitive name filtering can find an exact short/common title
        // buried behind dozens of unrelated fuzzy results (e.g. THE FINALS).
        // A returned identity is STILL subject to normal edition/platform review.
        internal List<CatalogGame> SearchExactTitle(string title, CancellationToken ct)
        {
            var query=CatalogCore.BuildExactNameQueryBody(title);
            if(String.IsNullOrEmpty(query)) return new List<CatalogGame>();
            string key="exact:"+query;
            List<CatalogGame> cached;
            if(cache.TryGetValue(key,out cached)) return cached;
            Authenticate(ct);
            for(int attempt=0;attempt<3;attempt++)
            {
                ct.ThrowIfCancellationRequested();
                using(var message=new HttpRequestMessage(HttpMethod.Post,"https://api.igdb.com/v4/games"))
                {
                    message.Headers.Add("Client-ID",clientId);
                    message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);
                    message.Content=new StringContent(query,Encoding.UTF8,"text/plain");
                    using(var response=http.SendAsync(message,ct).GetAwaiter().GetResult())
                    {
                        int status=(int)response.StatusCode;
                        bool retry=status==429 || status==502 || status==503 || status==504;
                        if(retry && attempt<2)
                        {
                            if(ct.WaitHandle.WaitOne(800*(attempt+1))) ct.ThrowIfCancellationRequested();
                            continue;
                        }
                        if(!response.IsSuccessStatusCode)
                            throw new InvalidOperationException("IGDB exact-title lookup returned HTTP "+status+
                                ". Read-only scan stopped; no Playnite changes made.");
                        var received=Parse<List<IgdbRecord>>(
                            response.Content.ReadAsStringAsync().GetAwaiter().GetResult()) ?? new List<IgdbRecord>();
                        var results=received.Where(x=>x!=null && x.Id>0 &&
                            String.Equals((x.Name??"").Trim(),title.Trim(),StringComparison.OrdinalIgnoreCase))
                            .GroupBy(x=>x.Id).Select(g=>g.First())
                            .Select(x=>new CatalogGame {
                                Id=x.Id, Name=x.Name, Summary=x.Summary,
                                FirstReleaseYear=ReleaseYear(x.FirstReleaseDate),
                                Platforms=(x.Platforms??new List<IgdbPlatform>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
                                PlatformIds=(x.Platforms??new List<IgdbPlatform>()).Where(a=>a!=null).Select(a=>a.Id).ToList(),
                                Aliases=(x.Aliases??new List<IgdbAlias>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
                                ReleaseDates=(x.ReleaseDates??new List<IgdbRelease>()).Where(r=>r!=null)
                                    .Select(r=>new CatalogRelease{PlatformId=r.Platform==null?0:r.Platform.Id,Year=ReleaseYear(r.Date)}).ToList(),
                                CoverImageId=x.Cover==null?"":x.Cover.ImageId,
                                ScreenshotImageId=x.Screenshots==null || x.Screenshots.Count==0 || x.Screenshots[0]==null
                                    ? "":x.Screenshots[0].ImageId
                            }).ToList();
                        cache[key]=results;
                        return results;
                    }
                }
            }
            throw new InvalidOperationException("IGDB exact-title lookup failed without a response.");
        }

        // One additional bounded, platform-specific read-only query for shortened
        // titles. Result length equal to the cap is treated as incomplete.
        internal List<CatalogGame> SearchTitlePrefix(string title,string platform,CancellationToken ct)
        {
            var query=CatalogSmartResolver.BuildPrefixQuery(title,platform);
            if(String.IsNullOrEmpty(query))return new List<CatalogGame>();
            var key="smart-prefix:"+query;
            List<CatalogGame> cached;
            if(cache.TryGetValue(key,out cached))return cached;
            Authenticate(ct);
            for(int attempt=0;attempt<3;attempt++)
            {
                ct.ThrowIfCancellationRequested();
                using(var message=new HttpRequestMessage(HttpMethod.Post,"https://api.igdb.com/v4/games"))
                {
                    message.Headers.Add("Client-ID",clientId);
                    message.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);
                    message.Content=new StringContent(query,Encoding.UTF8,"text/plain");
                    using(var response=http.SendAsync(message,ct).GetAwaiter().GetResult())
                    {
                        int status=(int)response.StatusCode;
                        if((status==429 || status==502 || status==503 || status==504) && attempt<2)
                        {
                            if(ct.WaitHandle.WaitOne(800*(attempt+1)))ct.ThrowIfCancellationRequested();
                            continue;
                        }
                        if(!response.IsSuccessStatusCode)
                            throw new InvalidOperationException("IGDB prefix investigation failed (HTTP "+status+").");
                        var found=Parse<List<IgdbRecord>>(response.Content.ReadAsStringAsync()
                            .GetAwaiter().GetResult())??new List<IgdbRecord>();
                        var result=found.Where(x=>x!=null && x.Id>0)
                            .GroupBy(x=>x.Id).Select(g=>g.First()).Select(x=>new CatalogGame {
                                Id=x.Id,Name=x.Name,Summary=x.Summary,
                                FirstReleaseYear=ReleaseYear(x.FirstReleaseDate),
                                GameTypeId=x.GameTypeId,VersionParentId=x.VersionParentId,
                                ParentGameId=x.ParentGameId,VersionTitle=x.VersionTitle,
                                RatingCount=x.TotalRatingCount.GetValueOrDefault(),
                                PlatformIds=(x.Platforms??new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Id).ToList(),
                                Platforms=(x.Platforms??new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Name).ToList(),
                                Aliases=(x.Aliases??new List<IgdbAlias>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
                                ReleaseDates=(x.ReleaseDates??new List<IgdbRelease>()).Where(r=>r!=null)
                                    .Select(r=>new CatalogRelease{PlatformId=r.Platform==null?0:r.Platform.Id,
                                        Year=ReleaseYear(r.Date)}).ToList(),
                                CoverImageId=x.Cover==null?"":x.Cover.ImageId,
                                ScreenshotImageId=x.Screenshots==null||x.Screenshots.Count==0||x.Screenshots[0]==null
                                    ?"":x.Screenshots[0].ImageId
                            }).ToList();
                        cache[key]=result;
                        if(ct.WaitHandle.WaitOne(310))ct.ThrowIfCancellationRequested();
                        return result;
                    }
                }
            }
            throw new InvalidOperationException("IGDB prefix investigation failed.");
        }

        private static List<string> ReadNames(IEnumerable<IgdbNamed> values)
        {
            return (values ?? Enumerable.Empty<IgdbNamed>()).Where(v=>v!=null)
                .Select(v=>(v.Name??"").Trim()).Where(v=>v.Length>0 && v.Length<=100)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToList();
        }
        // Fetch the exact IGDB identity selected by the user, not another fuzzy result.
        internal CatalogGame FetchById(long igdbId, CancellationToken ct)
        {
            if (igdbId <= 0) throw new ArgumentException("Invalid IGDB identity");
            Authenticate(ct);
            var query = "fields id,name,summary,platforms.id,platforms.name,alternative_names.name," +
                "cover.image_id,screenshots.image_id,first_release_date," +
                "game_type,version_parent,parent_game,version_title,total_rating_count," +
                "genres.name,involved_companies.company.name,involved_companies.developer," +
                "involved_companies.publisher,collections.name,release_dates.date,release_dates.platform.id; where id = " +
                igdbId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "; limit 1;";
            using (var message = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games"))
            {
                message.Headers.Add("Client-ID", clientId);
                message.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                message.Content = new StringContent(query, Encoding.UTF8, "text/plain");
                using (var response = http.SendAsync(message, ct).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException("IGDB identity verification failed (HTTP " +
                            (int)response.StatusCode + "). No metadata applied.");
                    var records = Parse<List<IgdbRecord>>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                    var x = (records ?? new List<IgdbRecord>()).FirstOrDefault(r => r != null && r.Id == igdbId);
                    if (x == null) throw new InvalidOperationException("The selected IGDB ID no longer exists.");
                    return new CatalogGame {
                        Id=x.Id, Name=x.Name, Summary=x.Summary,
                        GameTypeId=x.GameTypeId,VersionParentId=x.VersionParentId,
                        ParentGameId=x.ParentGameId,VersionTitle=x.VersionTitle,
                        RatingCount=x.TotalRatingCount.GetValueOrDefault(),
                        FirstReleaseYear=ReleaseYear(x.FirstReleaseDate),
                        Platforms=(x.Platforms ?? new List<IgdbPlatform>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
                        PlatformIds=(x.Platforms ?? new List<IgdbPlatform>()).Where(a=>a!=null).Select(a=>a.Id).ToList(),
                        Aliases=(x.Aliases ?? new List<IgdbAlias>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
                        CoverImageId=x.Cover==null?"":x.Cover.ImageId,
                        ScreenshotImageId=x.Screenshots==null || x.Screenshots.Count==0 || x.Screenshots[0]==null
                            ? "":x.Screenshots[0].ImageId,
                        Genres=ReadNames(x.Genres),
                        Developers=ReadNames((x.InvolvedCompanies ?? new List<IgdbCompanyRelation>())
                            .Where(c=>c!=null && c.Developer && c.Company!=null).Select(c=>c.Company)),
                        Publishers=ReadNames((x.InvolvedCompanies ?? new List<IgdbCompanyRelation>())
                            .Where(c=>c!=null && c.Publisher && c.Company!=null).Select(c=>c.Company)),
                        SeriesNames=ReadNames(x.Collections),
                        ReleaseDates=(x.ReleaseDates ?? new List<IgdbRelease>()).Where(r=>r!=null &&
                            r.Platform!=null && r.Platform.Id>0 && ReleaseYear(r.Date)>0)
                            .Select(r=>new CatalogRelease{PlatformId=r.Platform.Id,Year=ReleaseYear(r.Date)})
                            .ToList()
                    };
                }
            }
        }

        // Only approved IGDB image size presets; no untrusted hosts or arbitrary URLs.
        internal byte[] DownloadCover(string imageId, CancellationToken ct)
        {
            return DownloadImage(imageId,"t_cover_big_2x",ct);
        }
        internal byte[] DownloadIcon(string imageId, CancellationToken ct)
        {
            return DownloadImage(imageId,"t_thumb",ct);
        }
        internal byte[] DownloadBackground(string imageId, CancellationToken ct)
        {
            return DownloadImage(imageId,"t_screenshot_huge",ct);
        }
        private byte[] DownloadImage(string imageId,string size,CancellationToken ct)
        {
            if (size!="t_cover_big_2x" && size!="t_thumb" && size!="t_screenshot_huge")
                throw new InvalidOperationException("Unsupported IGDB image preset.");
            if (!MetadataPlan.SafeImageId(imageId))
                throw new InvalidOperationException("IGDB returned an invalid cover image ID.");
            var uri = "https://images.igdb.com/igdb/image/upload/" + size + "/" + imageId + ".jpg";
            using (var response = http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct)
                .GetAwaiter().GetResult())
            {
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("IGDB cover download failed (HTTP " +
                        (int)response.StatusCode + ").");
                var mediaType=response.Content.Headers.ContentType;
                if (mediaType == null || !String.Equals(mediaType.MediaType,"image/jpeg",StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("IGDB cover response is not JPEG.");
                if (response.Content.Headers.ContentLength.HasValue &&
                    response.Content.Headers.ContentLength.Value > 8*1024*1024)
                    throw new InvalidDataException("IGDB cover exceeds 8 MB safety limit.");
                using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                using (var output = new MemoryStream())
                {
                    var buffer=new byte[8192];
                    int read;
                    while ((read=stream.ReadAsync(buffer,0,buffer.Length,ct).GetAwaiter().GetResult())>0)
                    {
                        if (output.Length+read > 8*1024*1024)
                            throw new InvalidDataException("IGDB cover exceeded the 8 MB limit.");
                        output.Write(buffer,0,read);
                    }
                    var bytes=output.ToArray();
                    if (!MetadataPlan.LooksLikeJpeg(bytes))
                        throw new InvalidDataException("Downloaded image failed JPEG signature validation.");
                    return bytes;
                }
            }
        }
        public void Dispose() { http.Dispose(); }
    }
}
