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
        private static readonly Dictionary<string, string> KnownNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cladun This Is An RPG", "ClaDun: This is an RPG" },
            { "Cladun X 2", "ClaDun x2" },
            { "Final Fantasy VII Crisis Core", "Crisis Core: Final Fantasy VII" },
            { "Dragon Ball-Ad Adv", "Dragon Ball: Advanced Adventure" },
            { "Kirby Nightmare In Dreamland", "Kirby: Nightmare in Dream Land" },
            { "The king of fighter EX2", "The King of Fighters EX2" }
        };

        internal static string QueryTitle(string name, string platform)
        {
            var title = Spaces.Replace((name ?? "").Trim(), " ");
            var kind = PlatformKey(platform);
            if (kind == "gba" || kind == "gbc" || kind == "gb" || kind == "psp" || kind == "nds")
                title = Index.Replace(title, "");
            if (kind == "psp" && title.EndsWith(" PSP", StringComparison.OrdinalIgnoreCase))
                title = title.Substring(0, title.Length - 4).Trim();
            var article = Regex.Match(title, @"^(.*?),\s*(The|An|A)\s*([-:].*)?$", RegexOptions.IgnoreCase);
            if (article.Success)
                title = article.Groups[2].Value + " " + article.Groups[1].Value + article.Groups[3].Value;
            // Audited catalog alias: exact GBA launch title, not a generic 'Adv'
            // expansion. Other titles ending in Adv remain search-only discovery.
            if (kind == "gba" && Flat(title) == "supermarioadv")
                return "Super Mario Advance";
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
            string alias;
            return KnownNames.TryGetValue(title, out alias) ? alias : title;
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
            var key = Flat(conjunction);
            return key.EndsWith("version", StringComparison.Ordinal) && key.Length > 12
                ? key.Substring(0, key.Length - "version".Length) : key;
        }

        internal static string PlatformKey(string platform)
        {
            var x = Flat(platform);
            if (x.Contains("gameboyadvance") || x == "gba") return "gba";
            if (x.Contains("gameboycolor") || x == "gbc") return "gbc";
            if (x.Contains("gameboy") || x == "gb") return "gb";
            if (x.Contains("playstationportable") || x == "psp") return "psp";
            if (x.Contains("nintendods") || x == "nds") return "nds";
            if (x.Contains("nintendo3ds") || x == "3ds") return "3ds";
            if (x.Contains("pcwindows") || x.Contains("pcmicrosoftwindows") || x == "windows") return "pc";
            if (x == "playstation2") return "ps2";
            if (x == "playstation") return "ps1";
            if (x == "nintendoswitch") return "switch";
            return x;
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
                if(value.Length<3 || value.Length>100 || variants.Count>=4) return;
                if(variants.Any(v=>String.Equals(v.Text,value,StringComparison.OrdinalIgnoreCase))) return;
                variants.Add(new CatalogQuery { Text=value, StrongEvidence=trusted });
            };
            add(primary,true);
            var kind=PlatformKey(platform);
            var compact=Flat(primary);
            // Preserve the original version of a recognized alias for search only.
            var raw=Spaces.Replace((name??"").Trim()," ");
            if(primary!=raw) {
                var original=Index.Replace(raw,"");
                if(kind=="psp" && original.EndsWith(" PSP",StringComparison.OrdinalIgnoreCase))
                    original=original.Substring(0,original.Length-4).Trim();
                add(original,true);
            }
            // Punctuation and conjunction substitutions frequently repair provider search.
            var punctuation=CleanPunctuation(primary);
            if(!String.Equals(primary,punctuation,StringComparison.OrdinalIgnoreCase))
                add(punctuation,true);
            var ampersand=Regex.Replace(primary,@"\band\b","&",RegexOptions.IgnoreCase);
            if(!String.Equals(primary,ampersand,StringComparison.OrdinalIgnoreCase))
                add(ampersand,true);

            if(kind=="gba") {
                if(compact=="pokemonfirered") add("Pokemon FireRed Version",true);
                if(compact=="pokemonleafgreen") add("Pokemon LeafGreen Version",true);
                if(compact=="thekingoffightersex1" || compact=="thekingoffightersex")
                    add("The King of Fighters EX: Neo Blood",false);
                if(compact=="thekingoffightersex2" || compact=="thekingofthefighterex2")
                    add("The King of Fighters EX2: Howling Blood",false);
                // A game identified only as Mario Kart might be Super Circuit, but
                // the source name does not justify automatically selecting that edition.
                if(compact=="mariokart") add("Mario Kart: Super Circuit",false);
                if(Regex.IsMatch(primary,@"\bAdv$",RegexOptions.IgnoreCase))
                    add(Regex.Replace(primary,@"\bAdv$","Advance",RegexOptions.IgnoreCase),false);
            }
            // Limited discovery fallbacks for incomplete ROM-list titles. These
            // variants are NEVER identity evidence for unattended writes.
            if(kind=="gba" || kind=="gb" || kind=="gbc" || kind=="nds" || kind=="psp") {
                if(Regex.IsMatch(primary,@"\bAdv$",RegexOptions.IgnoreCase)) {
                    add(Regex.Replace(primary,@"\bAdv$","Adventure",RegexOptions.IgnoreCase),false);
                    add(Regex.Replace(primary,@"\bAdv$","Adventures",RegexOptions.IgnoreCase),false);
                }
                if(Regex.IsMatch(primary,@"\s+1$",RegexOptions.IgnoreCase))
                    add(Regex.Replace(primary,@"\s+1$",""),false);
                var sequence=Regex.Match(primary,@"^(?<series>.+?)\s+[12]\s*[-:]\s*(?<subtitle>.+)$");
                if(sequence.Success)
                    add(sequence.Groups["series"].Value+": "+sequence.Groups["subtitle"].Value,false);
            }
            if(kind=="gb" || kind=="gbc") {
                if(compact=="pokemonyellowversion" || compact=="pokemonyellow") {
                    add("Pokemon Yellow Version",true);
                    add("Pokemon Yellow Version: Special Pikachu Edition",false);
                    add("Pokemon Yellow",false);
                }
            }
            // In some combined titles the subtitle is the provider's main title.
            // A subtitle-only search is discovery evidence, never identity proof.
            var suffix=Regex.Match(primary,@"^(.*?)[\s:-]+(Super Mario Advance\s+[0-9]+)$",RegexOptions.IgnoreCase);
            if(suffix.Success) add(suffix.Groups[2].Value,false);
            var rev=Regex.Match(primary,@"^(.+?)\s*\((?:USA|Europe|Japan|Rev\s*[0-9]+)\)$",RegexOptions.IgnoreCase);
            if(rev.Success) add(rev.Groups[1].Value,false);
            // Read-only discovery for install/client/beta labels in the 318-game
            // audit. The base game is NEVER proven to be this exact edition.
            var extra=Regex.Match(primary,
                @"^(?<base>.+?)(?:\s*[-:]\s*|\s+)(?:Legacy(?:\s+Mode)?|(?:DS\s+)?Beta(?:\s+Test)?|Playtest|Starter\s+Access|Test|Launcher|Mod\s+SDK)$",
                RegexOptions.IgnoreCase);
            if(extra.Success) add(extra.Groups["base"].Value,false);
            var dlc=Regex.Match(primary,@"^(?<base>.+?)\s*\+\s*.+?\s+DLC$",RegexOptions.IgnoreCase);
            if(dlc.Success) add(dlc.Groups["base"].Value,false);
            // A one-off known PSP packaging title. Search-only, since the
            // original name lacks the exact edition.
            if(kind=="psp" && compact=="ysoneandtwo") add("Ys I & II Chronicles",false);
            return variants;
        }

        // Deterministic, injection-safe title request for IGDB /v4/games.
        // Do not store credentials, execute arbitrary APICalypse, or modify Playnite here.
        internal static string BuildSingleQueryBody(string phrase)
        {
            var safe = new StringBuilder();
            foreach (var c in phrase ?? "")
            {
                if (safe.Length >= 100) break;
                if (Char.IsLetterOrDigit(c) || c==' ' || c=='\'' || c=='-' || c==':' || c=='&') safe.Append(c);
            }
            var clean=Spaces.Replace(safe.ToString()," ").Trim();
            if (clean.Length < 3) return "";
            return "fields id,name,summary,platforms.id,platforms.name,alternative_names.name," +
                "cover.image_id,screenshots.image_id,first_release_date; search \"" + clean + "\"; limit 40;";
        }

        // Narrow automatic-batch gate. It is deliberately stricter than the
        // visual Strong classification, which is NOT a calibrated probability.
        // Only structured platform-ID and unique trusted exact-title evidence,
        // with a dated catalog record, can enter a one-confirmation auto plan.
        internal static bool CanAutoPlan(CatalogMatch match)
        {
            if(match==null || match.Input==null || match.Game==null ||
               match.MatchStatus!="Strong candidate" || !match.PlatformMatches ||
               match.Game.Id<=0 ||
               (match.Game.FirstReleaseYear<1970 && !match.UndatedExactContextEvidence) ||
               match.Score<0.995 || match.AliasOnlyUnrelatedTitle ||
               match.Game.PlatformIds==null) return false;
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
                    plain.Append(Char.IsLetterOrDigit(ch) ? ch : ' ');
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
            return Regex.IsMatch(title??"",
                @"(?:^|[\s:-])(?:beta(?:\s+test)?|playtest|legacy(?:\s+mode)?|starter\s+access|mod\s+sdk|launcher|dlc)(?:$|[\s:-])",
                RegexOptions.IgnoreCase);
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
            // Keep explicitly ambiguous series such as Mario Kart in review.
            bool catalogPlatformId=expectedPlatformId>0 && top.Game.PlatformIds!=null &&
                top.Game.PlatformIds.Contains(expectedPlatformId);
            bool undatedContext=top.Game.FirstReleaseYear<1970 && catalogPlatformId &&
                uniqueExact && first.Item3 && first.Item2>=0.99999 &&
                !String.Equals(TitleKey(normalized),"mariokart",StringComparison.Ordinal) &&
                !String.IsNullOrWhiteSpace(top.Game.Summary) &&
                top.Game.Summary.Trim().Length>=40;
            var generic=TitleKey(normalized)=="mariokart" ||
                (normalized.Length<12 && CleanPunctuation(normalized).Split(' ').Length<=2 &&
                 top.Game.FirstReleaseYear<1970 && !undatedContext);
            bool unrelatedAlias=first.Item3 &&
                !PlausiblyRelatedCanonicalTitle(normalized,top.Game.Name);
            bool nonBaseLabel=HasNonBaseEditionQualifier(normalized);
            bool high=top.PlatformMatches && first.Item2>=0.985 && first.Item3 &&
                uniqueExact && !generic && !unrelatedAlias && !nonBaseLabel;
            top.ExactTrustedTitleEvidence=first.Item3;
            top.UniqueExactSamePlatform=uniqueExact;
            top.StructuredPlatformEvidence=expectedPlatformId>0 &&
                top.Game.PlatformIds!=null && top.Game.PlatformIds.Contains(expectedPlatformId);
            top.GenericTitleAmbiguity=generic;
            top.AliasOnlyUnrelatedTitle=unrelatedAlias;
            top.UndatedExactContextEvidence=undatedContext;
            top.MatchStatus=high?"Strong candidate":"Review";
            top.Reason+="; "+(first.Item3?"exact title/alias evidence":"fuzzy/broad title evidence")+
                "; "+ordered.Count+" unique candidate(s); next-best "+top.RunnerUp.ToString("0.00",CultureInfo.InvariantCulture)+
                (exactPlatform.Count>1?"; multiple exact same-platform records need review":"")+
                (generic?"; short/generic title needs review":"")+
                (unrelatedAlias?"; exact alias but unrelated official title — verify edition before import":"")+
                (nonBaseLabel?"; beta/launcher/legacy/DLC qualifier requires manual edition review":"")+
                (undatedContext?"; date unavailable, exact platform/title with catalog context":"");
            return top;
        }
    }
}
