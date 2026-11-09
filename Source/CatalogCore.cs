using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartMetadataMatcher
{
    internal sealed class CatalogInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
        public int MissingCount { get; set; }
        public int MissingMask { get; set; }
        // Independently stored Playnite date, never inferred from proposed IGDB match.
        public int LocalReleaseYear { get; set; }
    }

    internal sealed class CatalogGame
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<string> Aliases { get; set; }
        public List<string> Platforms { get; set; }
        public List<long> PlatformIds { get; set; }
        public int FirstReleaseYear { get; set; }
        public string Summary { get; set; }
        public string CoverImageId { get; set; }
        public string ScreenshotImageId { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> SeriesNames { get; set; }
        public List<CatalogRelease> ReleaseDates { get; set; }
        // Read-only candidate diagnostic fields; never used as a write permission.
        public long? GameTypeId { get; set; }
        public long? VersionParentId { get; set; }
        public long? ParentGameId { get; set; }
        public string VersionTitle { get; set; }
        public int RatingCount { get; set; }
    }

    internal sealed class CatalogRelease
    {
        public long PlatformId { get; set; }
        public int Year { get; set; }
    }

    internal sealed class CatalogQuery
    {
        public string Text { get; set; }
        // A broad search can discover a candidate, but must not alone authorize a strong label.
        public bool StrongEvidence { get; set; }
    }

    internal sealed class CatalogMatch
    {
        public CatalogInput Input { get; set; }
        public CatalogGame Game { get; set; }
        public string MatchStatus { get; set; }
        public string Reason { get; set; }
        public double Score { get; set; }
        public double RunnerUp { get; set; }
        public bool PlatformMatches { get; set; }
        public int CandidatesFound { get; set; }
        public List<CatalogGame> Alternatives { get; set; }
        // Machine-readable identity evidence; Reason is presentation only.
        public bool ExactTrustedTitleEvidence { get; set; }
        public bool UniqueExactSamePlatform { get; set; }
        public bool StructuredPlatformEvidence { get; set; }
        public bool GenericTitleAmbiguity { get; set; }
        // Rare undated games can be fully identified by exact title, unique
        // structured platform ID and substantive catalog context. Never derived
        // from the score alone, and never valid for a known generic-series title.
        public bool UndatedExactContextEvidence { get; set; }
        // Exact provider aliases can refer to retitled, different or bootleg games.
        // A completely unrelated canonical title requires user review.
        public bool AliasOnlyUnrelatedTitle { get; set; }
        public double CanonicalTitleSimilarity { get; set; }
        // An official-title remaster/edition differs from the Playnite title.
        // Even an exact IGDB alias does not make that edition interchangeable.
        public bool EditionConflict { get; set; }
        // In-memory decision from complete bounded rival comparison; never saved as a scan approval.
        public bool SmartAutoEvidence { get; set; }
    }

    // Read-only identity resolution. Neither score nor a 'Strong candidate' label is a
    // calibrated correctness probability or permission to write to the Playnite database.
    internal static class CatalogCore
    {
        // Exclude known non-production testing clients from metadata discovery.
        // This is a catalog-queue rule only; do not change or remove Playnite games.
        internal static bool IsTestingOnly(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(name.Trim(), @"\bpublic\s+test(?:ing)?\s*$", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(name.Trim(), @"\bcommunity\s+test\s+environment\s*$", RegexOptions.IgnoreCase);
        }

        // Mode: 0 = 20, 1 = 50 (default), 2 = all eligible remaining.
        // Bounded count, so the caller never scans beyond its current queue.
        internal static int NextScanCount(int remaining, int mode)
        {
            if (remaining <= 0) return 0;
            if (mode == 0) return Math.Min(20, remaining);
            if (mode == 2) return remaining;
            return Math.Min(50, remaining);
        }
        private static readonly Regex Index = new Regex(@"^(?:0[1-9]|[1-9][0-9])[\s._-]+(?=\p{L})", RegexOptions.Compiled);
        private static readonly Regex Spaces = new Regex(@"\s+", RegexOptions.Compiled);
        internal static string QueryTitle(string name, string platform)
        {
            var title = Spaces.Replace((name ?? "").Trim(), " ");
            // A number or platform suffix can belong to the official title.
            // Discovery may try removing it, but identity checks retain it.
            var article = Regex.Match(title, @"^(.*?),\s*(The|An|A)\s*([-:].*)?$", RegexOptions.IgnoreCase);
            if (article.Success)
                title = article.Groups[2].Value + " " + article.Groups[1].Value + article.Groups[3].Value;
            // Remove a leading acronym only when the full next words independently
            // spell it (WDL - World Destruction League - Thunder Tanks).
            var prefixed = Regex.Match(title, @"^(?<abbr>[A-Za-z]{2,5})\s*[-:]\s*(?<full>.+)$",
                RegexOptions.CultureInvariant);
            if (prefixed.Success)
            {
                var acronym = prefixed.Groups["abbr"].Value.ToUpperInvariant();
                var words = Regex.Matches(prefixed.Groups["full"].Value, @"\p{L}+")
                    .Cast<Match>().Take(acronym.Length).Select(m => m.Value.Substring(0, 1).ToUpperInvariant());
                if (String.Equals(String.Concat(words), acronym, StringComparison.Ordinal))
                    title = prefixed.Groups["full"].Value;
            }
            return title;
        }

        private static string Flat(string value)
        {
            var s = (value ?? "").Normalize(NormalizationForm.FormD).ToLowerInvariant();
            var b = new StringBuilder();
            foreach (var c in s)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (Char.IsLetterOrDigit(c)) b.Append(c);
            }
            return b.ToString();
        }

        // IGDB sometimes appends 'Version' to an otherwise equivalent title.
        // This is for comparison only, not a rewrite of the user's visible title.
        private static string TitleKey(string name)
        {
            // A conjunction written as '&' versus 'and' is not a different game.
            // Keep this for matching only: never rewrite the user's display title.
            var conjunction = Regex.Replace(name ?? "", @"(?<=\p{L})\s*&\s*(?=\p{L})", " and ");
            var key = Flat(conjunction.Replace("+", " plus "));
            return key.EndsWith("version", StringComparison.Ordinal) && key.Length > 12
                ? key.Substring(0, key.Length - "version".Length) : key;
        }

        internal static string PlatformKey(string platform)
        {
            // Exact aliases only. Substring matching conflates multi-platform
            // records and custom names with a single console.
            var x = Flat(platform);
            switch(x)
            {
                case "nintendogameboyadvance": case "gameboyadvance": case "gba": return "gba";
                case "nintendogameboycolor": case "gameboycolor": case "gbc": return "gbc";
                case "nintendogameboy": case "gameboy": case "gb": return "gb";
                case "sonyplaystationportable": case "playstationportable": case "psp": return "psp";
                case "nintendods": case "nds": case "ds": return "nds";
                case "nintendo3ds": case "3ds": return "3ds";
                case "pcwindows": case "pcmicrosoftwindows": case "windows": case "pc": return "pc";
                case "sonyplaystation2": case "playstation2": case "ps2": return "ps2";
                case "sonyplaystation": case "playstation": case "ps1": return "ps1";
                case "nintendoswitch": case "switch": return "switch";
                default: return x;
            }
        }

        // Search leads are deliberately separate from trusted title evidence.
        // These transformations work on any platform, including unmapped ones.
        internal static string DiscoveryTitle(string name, string platform)
        {
            var title=QueryTitle(name,platform);
            title=Index.Replace(title, "");
            var suffix=Regex.Match(title,@"^(?<name>.+?)\s+(?<tag>[A-Za-z0-9]+)$");
            long platformId=PlatformId(platform);
            if(suffix.Success && platformId>0 && PlatformId(suffix.Groups["tag"].Value)==platformId)
                title=suffix.Groups["name"].Value.Trim();
            return title;
        }

        // A few stable platform IDs, used only for corroboration. No forced platform
        // restriction at query time: a different-platform result must stay visible for review.
        internal static long PlatformId(string platform)
        {
            switch (PlatformKey(platform))
            {
                case "pc": return 6;
                case "nds": return 20;
                case "gbc": return 22;
                case "gba": return 24;
                case "gb": return 33;
                case "3ds": return 37;
                case "psp": return 38;
                case "ps1": return 7;
                case "ps2": return 8;
                case "switch": return 130;
                default: return 0;
            }
        }

        private static string CleanPunctuation(string s)
        {
            return Spaces.Replace(Regex.Replace(s ?? "", @"[\p{P}\p{S}]+", " "), " ").Trim();
        }

        internal static List<CatalogQuery> QueryVariants(string name, string platform)
        {
            var primary=QueryTitle(name,platform);
            var variants=new List<CatalogQuery>();
            // IGDB search may treat spaces and punctuation differently. Keep those
            // alternatives; comparisons later canonicalize punctuation independently.
            Action<string,bool> add=(value,trusted)=> {
                value=Spaces.Replace((value??"").Trim()," ");
                if(value.Length<1 || value.Length>100 || variants.Count>=4) return;
                if(variants.Any(v=>String.Equals(v.Text,value,StringComparison.OrdinalIgnoreCase))) return;
                variants.Add(new CatalogQuery { Text=value, StrongEvidence=trusted });
            };
            add(primary,true);
            var discovery=DiscoveryTitle(name,platform);
            add(discovery,false);
            // Punctuation and conjunction substitutions frequently repair provider search.
            var punctuation=CleanPunctuation(primary);
            if(!String.Equals(primary,punctuation,StringComparison.OrdinalIgnoreCase))
                add(punctuation,primary.IndexOf('+')<0);
            var ampersand=Regex.Replace(primary,@"\band\b","&",RegexOptions.IgnoreCase);
            if(!String.Equals(primary,ampersand,StringComparison.OrdinalIgnoreCase))
                add(ampersand,true);
            // Search-only spelling alternatives: catalog results still require
            // their own exact-title and platform checks before import.
            var withoutArticle=Regex.Match(primary,@"^(?:The)\s+(?<rest>\p{L}{5,}(?:\s+.*)?)$",RegexOptions.IgnoreCase);
            if(withoutArticle.Success) add(withoutArticle.Groups["rest"].Value,false);
            var starring=Regex.Match(primary,@"^(?<base>.+?\bRemaster(?:ed)?)\s+Starring\s+.+$",RegexOptions.IgnoreCase);
            if(starring.Success) add(starring.Groups["base"].Value,false);

            // Limited discovery fallbacks for incomplete ROM-list titles. These
            // variants are NEVER identity evidence for unattended writes.
            if(Regex.IsMatch(discovery,@"\bAdv$",RegexOptions.IgnoreCase)) {
                add(Regex.Replace(discovery,@"\bAdv$","Advance",RegexOptions.IgnoreCase),false);
                add(Regex.Replace(discovery,@"\bAdv$","Adventure",RegexOptions.IgnoreCase),false);
                add(Regex.Replace(discovery,@"\bAdv$","Adventures",RegexOptions.IgnoreCase),false);
            }
            // A supplied installment is binding unless the catalog itself
            // supplies the complete local title as an alias. Discovery alone
            // cannot establish that a local ordinal is merely an annotation.
            var sequence=Regex.Match(discovery,@"^(?<series>.+?)\s+[0-9]+\s*[-:]\s*(?<subtitle>.+)$");
            if(sequence.Success)
                add(sequence.Groups["series"].Value+": "+sequence.Groups["subtitle"].Value,false);
            var rev=Regex.Match(primary,@"^(.+?)\s*\((?:USA|Europe|Japan|Rev\s*[0-9]+)\)$",RegexOptions.IgnoreCase);
            if(rev.Success) add(rev.Groups[1].Value,false);
            // A base-title search for install/client/beta labels is discovery only.
            var extra=Regex.Match(primary,
                @"^(?<base>.+?)(?:\s*[-:]\s*|\s+)(?:(?:DS\s+)?Beta(?:\s+Test)?|Playtest|Starter\s+Access|Test|Launcher|Mod\s+SDK)$",
                RegexOptions.IgnoreCase);
            if(extra.Success) add(extra.Groups["base"].Value,false);
            // 'Legacy' can be part of a game's actual name (Hogwarts Legacy).
            // Strip it only with an explicit edition separator or 'Legacy Mode'.
            var legacy=Regex.Match(primary,
                @"^(?<base>.+?)(?:\s*[-:]\s*Legacy(?:\s+Mode)?|\s+Legacy\s+Mode)$",
                RegexOptions.IgnoreCase);
            if(legacy.Success) add(legacy.Groups["base"].Value,false);
            // A trailing Legacy without a separator may indicate a separate client
            // (Human: Fall Flat Legacy). Require three base-title words to avoid
            // broad queries for ordinary two-word titles such as Hogwarts Legacy.
            // This variant is discovery-only, never proof of edition equivalence.
            var bareLegacy=Regex.Match(primary,@"^(?<base>.+?)\s+Legacy$",RegexOptions.IgnoreCase);
            if(bareLegacy.Success && Regex.Matches(CleanPunctuation(bareLegacy.Groups["base"].Value),
                @"\p{L}+").Count>=3)
                add(bareLegacy.Groups["base"].Value,false);
            var dlc=Regex.Match(primary,@"^(?<base>.+?)\s*\+\s*.+?\s+DLC$",RegexOptions.IgnoreCase);
            if(dlc.Success) add(dlc.Groups["base"].Value,false);
            return variants;
        }

        // Deterministic, injection-safe title request for IGDB /v4/games.
        // Do not store credentials, execute arbitrary APICalypse, or modify Playnite here.
        internal static string BuildSingleQueryBody(string phrase)
        {
            var raw = phrase ?? "";
            var safe = new StringBuilder();
            for (var i=0; i<raw.Length && safe.Length<100; i++)
            {
                var c=raw[i];
                // Keep a slash only inside a title token (X/X-2, I/II).
                // Never preserve paired slashes or comment-like fragments.
                var titleSlash=c=='/' && i>0 && i+1<raw.Length &&
                    Char.IsLetterOrDigit(raw[i-1]) && Char.IsLetterOrDigit(raw[i+1]);
                if (Char.IsLetterOrDigit(c) || c==' ' || c=='\'' || c=='-' || c==':' || c=='&' || c=='+' || titleSlash)
                    safe.Append(c);
            }
            var clean=Spaces.Replace(safe.ToString()," ").Trim();
            if (clean.Length < 1) return "";
            return "fields id,name,summary,platforms.id,platforms.name,alternative_names.name," +
                "cover.image_id,screenshots.image_id,first_release_date,game_type,version_parent," +
                "parent_game,version_title,total_rating_count,release_dates.date,release_dates.platform.id; search \"" + clean + "\"; limit 40;";
        }

        // Exact-name fallback is only used for read-only discovery when the
        // ordinary search returned no actionable result. User text cannot inject
        // additional APICalypse commands. A match still goes through Choose().
        internal static string BuildExactNameQueryBody(string phrase)
        {
            if(String.IsNullOrWhiteSpace(phrase) || phrase.Length>100) return "";
            // Reject rather than rewrite names: this is an exact provider-name
            // filter and must never silently broaden the requested identity.
            if(phrase.Any(c=>!(Char.IsLetterOrDigit(c) || c==' ' || c=='-' || c==':' ||
                c=='&' || c=='/' || c=='\'' || c=='.' || c=='+' || c=='!' ||
                c=='?' || c==',' || c=='(' || c==')'))) return "";
            var value=Spaces.Replace(phrase.Trim()," ");
            if(value.Length<1)return "";
            return "fields id,name,summary,platforms.id,platforms.name,alternative_names.name,"+
                "cover.image_id,screenshots.image_id,first_release_date,game_type,version_parent,"+
                "parent_game,version_title,total_rating_count,release_dates.date,release_dates.platform.id; where name ~ \""+value+"\"; limit "+CatalogSmartResolver.MaxLookupResults+";";
        }

        // Narrow automatic-batch gate. It is deliberately stricter than the
        // visual Strong classification, which is NOT a calibrated probability.
        // Only structured platform-ID and unique trusted exact-title evidence,
        // with a dated catalog record, can enter a one-confirmation auto plan.
        internal static bool CanAutoPlan(CatalogMatch match)
        {
            // Smart resolutions use independently checked rival ranking. The live
            // writer has a separate exact-ID recheck for these fresh-only decisions.
            if(match==null || !CatalogSmartResolver.LiveIdentityCompatible(match.Input,match.Game))return false;
            if(match!=null && match.SmartAutoEvidence && match.Input!=null &&
                match.Game!=null && match.MatchStatus=="Strong candidate" &&
                match.StructuredPlatformEvidence && match.PlatformMatches &&
                !match.AliasOnlyUnrelatedTitle && !match.EditionConflict &&
                !match.GenericTitleAmbiguity && match.Game.Id>0) return true;
            if(match==null || match.Input==null || match.Game==null ||
               match.MatchStatus!="Strong candidate" || !match.PlatformMatches ||
               match.Game.Id<=0 ||
               (match.Game.FirstReleaseYear<1970 && !match.UndatedExactContextEvidence) ||
               match.Score<0.995 || match.AliasOnlyUnrelatedTitle ||
               match.EditionConflict || match.Game.PlatformIds==null) return false;
            long platformId=PlatformId(match.Input.Platform);
            if(platformId<=0 || !match.Game.PlatformIds.Contains(platformId)) return false;
            return match.StructuredPlatformEvidence &&
                match.ExactTrustedTitleEvidence &&
                match.UniqueExactSamePlatform && !match.GenericTitleAmbiguity;
        }

        // Some catalog titles place the main game and edition subtitle in the
        // opposite order. Identical complete token multisets (including digits),
        // rather than a generic token-overlap threshold, are usable evidence.
        // A high score still requires the separate unique-ID/platform checks.
        private static bool ExactReorderedTokens(string left, string right)
        {
            Func<string,List<string>> tokens = value => {
                var normalized = (value ?? "").Normalize(NormalizationForm.FormD).ToLowerInvariant();
                var plain = new StringBuilder();
                foreach (var ch in normalized)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                    if(ch=='+')plain.Append(" plus ");
                    else plain.Append(Char.IsLetterOrDigit(ch) ? ch : ' ');
                }
                return Regex.Matches(plain.ToString(), @"[\p{L}\p{N}]+")
                    .Cast<Match>().Select(m => m.Value).OrderBy(t => t, StringComparer.Ordinal).ToList();
            };
            var a = tokens(left);
            if (a.Count < 5) return false;
            var b = tokens(right);
            return a.Count == b.Count && a.SequenceEqual(b, StringComparer.Ordinal);
        }

        private static double TextSimilarity(string a, string b)
        {
            if (ExactReorderedTokens(a,b)) return 1;
            a = TitleKey(a); b = TitleKey(b);
            if (a.Length==0 || b.Length==0) return 0;
            if (a==b) return 1;
            var previous=Enumerable.Range(0,b.Length+1).ToArray();
            var current=new int[b.Length+1];
            for(int i=1;i<=a.Length;i++)
            {
                current[0]=i;
                for(int j=1;j<=b.Length;j++)
                    current[j]=Math.Min(Math.Min(previous[j]+1,current[j-1]+1),previous[j-1]+(a[i-1]==b[j-1]?0:1));
                var temp=previous;previous=current;current=temp;
            }
            return Math.Max(0,1-(double)previous[b.Length]/Math.Max(a.Length,b.Length));
        }

        // IGDB alternative_names may link a game to a local/bootleg title that
        // is *not* the same edition. Treat wholly unrelated official names as
        // review candidates even when the alternative-name similarity is 1.0.
        // This intentionally errs on the side of manual review for translations.
        private static bool HasNonBaseEditionQualifier(string title)
        {
            var text=title??"";
            return Regex.IsMatch(text,
                @"(?:^|[\s:-])(?:beta(?:\s+test)?|playtest|starter\s+access|mod\s+sdk|launcher|dlc)(?:$|[\s:-])",
                RegexOptions.IgnoreCase) ||
                Regex.IsMatch(text,@"(?:[-:]\s*legacy(?:\s+mode)?|\s+legacy\s+mode)\s*$",
                    RegexOptions.IgnoreCase);
        }

        private static bool HasMaterialEditionDifference(string requested,string official)
        {
            var source=TitleKey(requested);
            var target=TitleKey(official);
            if(source==target) return false;
            // A local title that adds an unexplained suffix may be an alternate release.
            if(target.Length>=4 && source.StartsWith(target,StringComparison.Ordinal))return true;
            // A matching platform and an exact alias are not enough to collapse
            // a remaster, edition or client build into the base game's identity.
            const string marker=@"(?:^|[^\p{L}\p{N}])(?:remaster(?:ed)?|redux|definitive|deluxe|ultimate|anniversary|director'?s\s+cut|edition)(?:$|[^\p{L}\p{N}])";
            bool inputHas=Regex.IsMatch(requested??"",marker,RegexOptions.IgnoreCase);
            bool officialHas=Regex.IsMatch(official??"",marker,RegexOptions.IgnoreCase);
            return inputHas!=officialHas;
        }

        private static bool PlausiblyRelatedCanonicalTitle(string inputTitle,string officialTitle)
        {
            var original=TitleKey(inputTitle);
            var official=TitleKey(officialTitle);
            if(original.Length>=4 && official.Contains(original))return true;
            if(official.Length>=4 && original.Contains(official))return true;
            var a=new HashSet<string>(Regex.Matches(CleanPunctuation(inputTitle??"").ToLowerInvariant(),@"\p{L}{4,}")
                .Cast<Match>().Select(m=>m.Value),StringComparer.Ordinal);
            var b=new HashSet<string>(Regex.Matches(CleanPunctuation(officialTitle??"").ToLowerInvariant(),@"\p{L}{4,}")
                .Cast<Match>().Select(m=>m.Value),StringComparer.Ordinal);
            // Words such as 'Game', 'Version', and 'Edition' cannot corroborate an identity.
            a.ExceptWith(new[]{"game","version","edition","super","the"});
            b.ExceptWith(new[]{"game","version","edition","super","the"});
            if(a.Overlaps(b))return true;
            return TextSimilarity(inputTitle,officialTitle)>=0.62;
        }

        // Review-only anchor for partial titles, publisher/franchise prefixes,
        // long subtitles, and fully reversed title words. Never score evidence
        // for automatic import: the trusted exact-title gate remains unchanged.
        private static bool PlausibleManualReviewAnchor(string requested,string official)
        {
            var source=TitleKey(requested);
            var target=TitleKey(official);
            if(source.Length>=8 && target.Contains(source)) return true;
            Func<string,List<string>> words = value=>Regex.Matches(
                CleanPunctuation(value??"").ToLowerInvariant(),@"[\p{L}\p{N}]+")
                .Cast<Match>().Select(m=>m.Value).OrderBy(s=>s,StringComparer.Ordinal).ToList();
            var a=words(requested);
            return a.Count>=3 && a.SequenceEqual(words(official),StringComparer.Ordinal);
        }

        internal static CatalogMatch Choose(CatalogInput input, IEnumerable<CatalogGame> games)
        {
            var variants=QueryVariants(input.Name,input.Platform);
            var key=PlatformKey(input.Platform);
            var expectedPlatformId=PlatformId(input.Platform);
            var choices=new List<Tuple<CatalogMatch,double,bool>>();
            foreach(var game in (games ?? Enumerable.Empty<CatalogGame>()).Where(g=>g!=null && g.Id>0)
                .GroupBy(g=>g.Id).Select(g=>g.First()))
            {
                var names=new List<string> { game.Name ?? "" };
                if(game.Aliases!=null) names.AddRange(game.Aliases.Where(x=>!String.IsNullOrWhiteSpace(x)));
                double bestSimilarity=0;
                bool exactTrusted=false;
                foreach(var variant in variants)
                    foreach(var n in names)
                    {
                        var sim=TextSimilarity(variant.Text,n);
                        if(sim>bestSimilarity) bestSimilarity=sim;
                        if(sim==1 && variant.StrongEvidence) exactTrusted=true;
                    }
                // Canonical name relevance breaks score ties before catalog ID.
                // An exact provider alias is not automatically better than an
                // edition-specific official name (e.g., Final Fantasy II vs IV).
                double canonicalSimilarity=variants.Select(v=>TextSimilarity(v.Text,game.Name))
                    .DefaultIfEmpty(0).Max();
                bool hasIds=game.PlatformIds!=null && game.PlatformIds.Count>0;
                var listed=(game.Platforms ?? new List<string>()).Select(PlatformKey).ToList();
                bool platformMatches=expectedPlatformId>0 && hasIds
                    ? game.PlatformIds.Contains(expectedPlatformId)
                    : key.Length>0 && listed.Contains(key);
                bool hasPlatformEvidence=hasIds || listed.Count>0;
                double score=bestSimilarity*0.78+(platformMatches?0.22:0);
                if(hasPlatformEvidence && !platformMatches) score=Math.Max(0,score-0.12);
                var reason=(platformMatches?(hasIds?"Platform ID corroborated":"Platform name corroborated"):
                    (hasPlatformEvidence?"Platform mismatch":"Platform unknown"))+
                    "; title similarity "+bestSimilarity.ToString("0.00",CultureInfo.InvariantCulture);
                choices.Add(Tuple.Create(new CatalogMatch {
                    Input=input,Game=game,Score=score,PlatformMatches=platformMatches,Reason=reason,
                    CanonicalTitleSimilarity=canonicalSimilarity,Alternatives=new List<CatalogGame>()
                },bestSimilarity,exactTrusted));
            }
            // With a known platform, a plausible same-platform release outranks a
            // literal-title match on the wrong system. Still classify it as Review
            // unless independent evidence establishes the exact edition.
            var inputKey=TitleKey(QueryTitle(input.Name,input.Platform));
            Func<Tuple<CatalogMatch,double,bool>,bool> plausiblePlatform=x=>
                x.Item1.PlatformMatches &&
                (x.Item2>=0.55 || (inputKey.Length>=8 &&
                  TitleKey(x.Item1.Game.Name).StartsWith(inputKey,StringComparison.Ordinal)));
            bool hasPlausiblePlatform=choices.Any(plausiblePlatform);
            var ordered=choices.OrderByDescending(x=>hasPlausiblePlatform && plausiblePlatform(x))
                .ThenByDescending(x=>x.Item1.Score)
                .ThenByDescending(x=>x.Item1.CanonicalTitleSimilarity)
                .ThenBy(x=>x.Item1.Game.Id).ToList();
            if(ordered.Count==0)
                return new CatalogMatch { Input=input, MatchStatus="Unmatched",Reason="No IGDB candidate returned by "+variants.Count+" search variant(s)",Alternatives=new List<CatalogGame>() };
            var first=ordered[0];
            // An irrelevant fuzzy IGDB result is not a useful proposed match.
            // Keep the result unmatched and let the user search manually.
            bool lowSimilarity=first.Item2<0.55 && !first.Item3;
            bool manualAnchor=false;
            if(lowSimilarity)
            {
                // Require a structured platform ID AND substantial official-title
                // corroboration. Aliases alone are insufficient. This path can
                // produce Review only, never a Strong or unattended match.
                var requested=QueryTitle(input.Name,input.Platform);
                var review=ordered.FirstOrDefault(x=>expectedPlatformId>0 &&
                    x.Item1.Game.PlatformIds!=null &&
                    x.Item1.Game.PlatformIds.Contains(expectedPlatformId) &&
                    (PlausibleManualReviewAnchor(requested,x.Item1.Game.Name) ||
                     PlausibleManualReviewAnchor(DiscoveryTitle(input.Name,input.Platform),x.Item1.Game.Name)));
                if(review==null)
                    return new CatalogMatch { Input=input,MatchStatus="Unmatched",
                        Reason="IGDB returned candidates, but none has sufficient title relevance; best similarity "+
                            first.Item2.ToString("0.00",CultureInfo.InvariantCulture)+". Search manually for the correct identity.",
                        CandidatesFound=ordered.Count,Alternatives=ordered.Take(3).Select(x=>x.Item1.Game).ToList() };
                ordered.Remove(review);ordered.Insert(0,review);
                first=review;
                manualAnchor=true;
            }

            var top=first.Item1;
            top.CandidatesFound=ordered.Count;
            top.Alternatives=ordered.Skip(1).Take(3).Select(x=>x.Item1.Game).ToList();
            top.RunnerUp=ordered.Count>1?ordered[1].Item1.Score:0;
            var normalized=QueryTitle(input.Name,input.Platform);
            // A nearby sequel should NOT override one unique exact match on the
            // correct platform. Multiple exact same-platform IGDB IDs must be reviewed.
            var exactPlatform=ordered.Where(x=>x.Item1.PlatformMatches && x.Item2>=0.99999 && x.Item3).ToList();
            bool uniqueExact=exactPlatform.Count==1 && exactPlatform[0].Item1.Game.Id==top.Game.Id;
            // An unknown release date is absence of evidence, not a conflicting
            // platform. Undated short titles need additional catalog context.
            bool catalogPlatformId=expectedPlatformId>0 && top.Game.PlatformIds!=null &&
                top.Game.PlatformIds.Contains(expectedPlatformId);
            bool undatedContext=top.Game.FirstReleaseYear<1970 && catalogPlatformId &&
                uniqueExact && first.Item3 && first.Item2>=0.99999 &&
                !String.IsNullOrWhiteSpace(top.Game.Summary) &&
                top.Game.Summary.Trim().Length>=40;
            var generic=normalized.Length<12 && CleanPunctuation(normalized).Split(' ').Length<=2 &&
                top.Game.FirstReleaseYear<1970 && !undatedContext;
            bool unrelatedAlias=first.Item3 &&
                !PlausiblyRelatedCanonicalTitle(normalized,top.Game.Name);
            bool nonBaseLabel=HasNonBaseEditionQualifier(normalized);
            bool editionConflict=HasMaterialEditionDifference(normalized,top.Game.Name);
            bool high=top.PlatformMatches && first.Item2>=0.985 && first.Item3 &&
                uniqueExact && !generic && !unrelatedAlias && !nonBaseLabel && !editionConflict;
            top.ExactTrustedTitleEvidence=first.Item3;
            top.UniqueExactSamePlatform=uniqueExact;
            top.StructuredPlatformEvidence=expectedPlatformId>0 &&
                top.Game.PlatformIds!=null && top.Game.PlatformIds.Contains(expectedPlatformId);
            top.GenericTitleAmbiguity=generic;
            top.AliasOnlyUnrelatedTitle=unrelatedAlias;
            top.EditionConflict=editionConflict;
            top.UndatedExactContextEvidence=undatedContext;
            top.MatchStatus=high?"Strong candidate":"Review";
            top.Reason+="; "+(first.Item3?"exact title/alias evidence":"fuzzy/broad title evidence")+
                "; "+ordered.Count+" unique candidate(s); next-best "+top.RunnerUp.ToString("0.00",CultureInfo.InvariantCulture)+
                (exactPlatform.Count>1?"; multiple exact same-platform records need review":"")+
                (generic?"; short/generic title needs review":"")+
                (unrelatedAlias?"; exact alias but unrelated official title — verify edition before import":"")+
                (nonBaseLabel?"; beta/launcher/legacy/DLC qualifier requires manual edition review":"")+
                (editionConflict?"; official title differs in remaster/edition/client qualifier — verify identity":"")+
                (undatedContext?"; date unavailable, exact platform/title with catalog context":"")+
                (manualAnchor?"; low-similarity same-platform title anchor — review manually; never auto-import":"");
            return top;
        }
    }
}
