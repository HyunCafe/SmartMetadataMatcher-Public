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

    // Fixed official endpoints only. Tokens stay in memory; secrets are never logged or exported.
    internal sealed class IgdbReadOnly : IDisposable
    {
        private readonly HttpClient http = new HttpClient();
        private readonly string clientId;
        private readonly string secret;
        private string token;
        // In-memory cache for this scan batch only; never persists user library titles.
        private readonly Dictionary<string,List<CatalogGame>> cache =
            new Dictionary<string,List<CatalogGame>>(StringComparer.Ordinal);
        internal IgdbReadOnly(string id, string secretValue)
        {
            clientId = (id ?? "").Trim();
            secret = (secretValue ?? "").Trim();
            if (clientId.Length == 0 || secret.Length == 0)
                throw new InvalidOperationException("Configure IGDB credentials first.");
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
                    throw new InvalidOperationException("Twitch authentication failed (HTTP " + (int)response.StatusCode + "). Verify local credentials and app configuration.");
                var auth = Parse<IgdbAuth>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                if (auth == null || String.IsNullOrWhiteSpace(auth.Token))
                    throw new InvalidOperationException("Twitch returned no usable access token.");
                token = auth.Token;
            }
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
                                    FirstReleaseYear=ReleaseYear(x.FirstReleaseDate),
                                    Platforms=(x.Platforms ?? new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Name).ToList(),
                                    PlatformIds=(x.Platforms ?? new List<IgdbPlatform>()).Where(p=>p!=null).Select(p=>p.Id).ToList(),
                                    Aliases=(x.Aliases ?? new List<IgdbAlias>()).Where(a=>a!=null).Select(a=>a.Name).ToList(),
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
