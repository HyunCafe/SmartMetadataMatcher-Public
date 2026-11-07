using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartMetadataMatcher
{
    // Evidence-based resolver for freshly searched titles only. Scores below are
    // relative ordering points, NEVER calibrated correctness percentages.
    // Neither this class nor the scan writes Playnite metadata.
    internal static class CatalogSmartResolver
    {
        internal const int MaxLookupResults = 101; // 100 plus sentinel for truncation.
        private sealed class Ranked
        {
            internal CatalogGame Game;
            internal int Kind; // 3 canonical, 2 related alias, 1 unique incomplete subtitle.
            internal int Points;
            internal int Year;
        }
        private static readonly Regex Words = new Regex(@"[\p{L}\p{N}]+",RegexOptions.Compiled);
        private static readonly Regex Edition = new Regex(
            @"\b(beta|playtest|test|demo|trial|launcher|sdk|legacy|remaster(?:ed)?|remake|definitive|complete|deluxe|gold|ultimate|anniversary|collection|compilation|premium|collector(?:s)?|enhanced|special|hd|goty|dlc|expansion)\b",
            RegexOptions.IgnoreCase|RegexOptions.Compiled);
        // Only normalize numerals as *game ordinals*. An English pronoun I,
        // Mega Man X (a separate franchise), and generic words such as One
        // must not become numeric evidence for a different title.
        private static readonly Dictionary<string,string> Roman = new Dictionary<string,string> {
            {"i","1"},{"ii","2"},{"iii","3"},{"iv","4"},{"v","5"},
            {"vi","6"},{"vii","7"},{"viii","8"},{"ix","9"},{"x","10"},
            {"xi","11"},{"xii","12"},{"xiii","13"},{"xiv","14"},{"xv","15"},
            {"xvi","16"},{"xvii","17"},{"xviii","18"},{"xix","19"},{"xx","20"}
        };
        private static readonly Dictionary<string,string> NumberWords = new Dictionary<string,string> {
            {"one","1"},{"two","2"},{"three","3"},{"four","4"},
            {"five","5"},{"six","6"},{"seven","7"},{"eight","8"},
            {"nine","9"},{"ten","10"}
        };
        private static readonly HashSet<string> OrdinalHeadings = new HashSet<string>(StringComparer.Ordinal) {
            "ys", "final fantasy", "dragon quest", "mortal kombat", "resident evil",
            "the elder scrolls", "elder scrolls", "the legend of heroes", "legend of heroes",
            "persona", "suikoden", "diablo", "kingdom hearts", "mass effect",
            "the witcher", "witcher", "quake", "doom", "fire emblem", "trails",
            "super mario advance", "megaman", "baldur s gate"
        };
        private static readonly HashSet<string> OrdinalLabels = new HashSet<string>(StringComparer.Ordinal) {
            "chapter", "episode", "part", "book", "volume", "vol", "season", "act"
        };
        private static bool OrdinalContext(List<string> tokens,int index)
        {
            if(index<=0)return false;
            var before=String.Join(" ",tokens.Take(index));
            if(OrdinalHeadings.Contains(before) || OrdinalLabels.Contains(tokens[index-1]))return true;
            // Franchise with preceding article or region tag.
            return OrdinalHeadings.Any(s=>before.EndsWith(" "+s,StringComparison.Ordinal));
        }
        private static bool IsSingleRomanOrdinal(List<string> tokens,int index)
        {
            if(OrdinalContext(tokens,index))return true;
            // Standalone trailing I in e.g. 'Game I' can denote an installment;
            // connector words guard commonplace pronoun uses ('You and I').
            if(tokens[index]=="i" && index==tokens.Count-1 && index>0 &&
               !new[]{"and","or","with","that","if","when","but","as"}
                    .Contains(tokens[index-1]))return true;
            return false;
        }
        private static bool IsNumberToken(string token)
        {return token.Length>0 && token.All(Char.IsDigit);}
        private static readonly HashSet<string> BroadFranchises = new HashSet<string>(StringComparer.Ordinal) {
            "mario kart", "final fantasy", "mission impossible", "need for speed",
            "dragon ball", "pokemon", "legend of heroes", "the legend of heroes",
            "naruto", "crash bandicoot", "monster hunter", "sonic the hedgehog"
        };
        internal static List<string> Tokens(string title)
        {
            var b=new StringBuilder();
            foreach(var c in (title??"").Normalize(NormalizationForm.FormD))
            {
                if(CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark)continue;
                // Plus is part of some official edition identities (Ys I & II
                // Chronicles+, N++), not interchangeable punctuation.
                if(c=='+'){b.Append(" plus ");continue;}
                b.Append(Char.IsLetterOrDigit(c)?Char.ToLowerInvariant(c):' ');
            }
            var values=Words.Matches(b.ToString()).Cast<Match>()
                .Select(x=>x.Value).ToList();
            // Detect ordinal *positions* using original tokens. Never convert
            // the English pronoun I in a running sentence or an isolated X.
            var original=values.ToList();
            for(int i=1;i<values.Count;i++)
            {
                string converted;
                if(Roman.TryGetValue(original[i],out converted))
                {
                    // I / V / X are ordinary letters and franchise labels too.
                    if(original[i].Length==1 && !IsSingleRomanOrdinal(original,i))continue;
                    if(original[i]=="x" && (String.Join(" ",original.Take(i))=="mega man" ||
                        String.Join(" ",original.Take(i))=="megaman"))continue;
                    values[i]=converted;
                }
                else if(NumberWords.TryGetValue(original[i],out converted) &&
                    (OrdinalContext(original,i) ||
                     (i>=3 && original[i-1]=="and" &&
                      NumberWords.ContainsKey(original[i-2]) &&
                      OrdinalContext(original,i-2))))values[i]=converted;
            }
            // 'Ys One and Two' / 'Ys I & II' must have the same identity key.
            // Keep 'and' elsewhere; it can be part of a game's official title.
            for(int i=values.Count-2;i>=1;i--)
                if(values[i]=="and" && IsNumberToken(values[i-1]) &&
                    IsNumberToken(values[i+1]))values.RemoveAt(i);
            return values;
        }
        private static bool EqualTokens(List<string> a,List<string> b)
        {return a.Count>0 && a.SequenceEqual(b,StringComparer.Ordinal);}
        private static bool StartsWith(List<string> full,List<string> part)
        {return part.Count>0 && full.Count>part.Count && full.Take(part.Count).SequenceEqual(part,StringComparer.Ordinal);}
        // Only repair very small text transcription differences. Numeric release
        // tokens must match exactly; sequel, DLC and edition words are never stripped.
        private static bool NearSameFullTitle(List<string> a,List<string> b)
        {
            if(a.Count<3 || b.Count<3)return false;
            var numsA=a.Where(x=>x.All(Char.IsDigit)).ToList();
            var numsB=b.Where(x=>x.All(Char.IsDigit)).ToList();
            if(!numsA.SequenceEqual(numsB,StringComparer.Ordinal))return false;
            if(a.Intersect(b,StringComparer.Ordinal).Count()<3)return false;
            var left=String.Concat(a);var right=String.Concat(b);
            if(left.Length<14 || right.Length<14 || left.Length>100 || right.Length>100)return false;
            if(Math.Abs(left.Length-right.Length)>Math.Max(left.Length,right.Length)*0.12)return false;
            var prev=new int[right.Length+1];var curr=new int[right.Length+1];
            for(int j=0;j<=right.Length;j++)prev[j]=j;
            for(int i=1;i<=left.Length;i++)
            {
                curr[0]=i;
                for(int j=1;j<=right.Length;j++)
                    curr[j]=Math.Min(Math.Min(prev[j]+1,curr[j-1]+1),
                        prev[j-1]+(left[i-1]==right[j-1]?0:1));
                var swap=prev;prev=curr;curr=swap;
            }
            return prev[right.Length]*1.0/Math.Max(left.Length,right.Length)<=0.12;
        }
        private static bool NumericIdentityCompatible(List<string> a,List<string> b)
        {
            // Treat EX2, IV, 3 and other numbered identity tokens as binding.
            Func<List<string>,List<string>> nums=words=>words.Where(x=>x.Any(Char.IsDigit))
                .ToList();
            return nums(a).SequenceEqual(nums(b),StringComparer.Ordinal);
        }
        private static bool FlexibleNumberedPrefix(List<string> full,List<string> partial)
        {
            if(partial.Count<4 || !partial.Any(x=>x.Any(Char.IsDigit)))return false;
            Func<List<string>,List<string>> comparable=words=>words
                .Where((x,i)=>i==0 || (x!="the" && x!="a" && x!="an"))
                .Select(x=>x.Length>=6 && x.EndsWith("s",StringComparison.Ordinal)
                    ?x.Substring(0,x.Length-1):x).ToList();
            var a=comparable(full);var b=comparable(partial);
            return NumericIdentityCompatible(b,a.Take(b.Count).ToList()) &&
                StartsWith(a,b);
        }
        private static int TitleKind(string requested,string official,IEnumerable<string> aliases)
        {
            var local=Tokens(requested);var name=Tokens(official);
            if(EqualTokens(local,name))return 3;
            // A catalog +/++ suffix can denote a distinct port or version.
            // Never infer missing '+' from an approximate or prefix match.
            if(local.Contains("plus")!=name.Contains("plus"))return 0;
            // An unrelated canonical name with an exact alias may be bootleg,
            // translated, or incorrectly indexed. Require shared canonical words.
            if((aliases??Enumerable.Empty<string>()).Any(x=>EqualTokens(local,Tokens(x))))
            {
                int common=local.Intersect(name,StringComparer.Ordinal).Count();
                if(common>=2 && common*2>=Math.Min(local.Count,name.Count))return 2;
            }
            // Informal "Part 1" naming may refer to an unnumbered first game.
            // Only the numeral ONE may be dropped; other sequels remain distinct.
            if(local.Count>=4 && local[local.Count-1]=="1" &&
                EqualTokens(local.Take(local.Count-1).ToList(),name) &&
                !Edition.IsMatch(requested))return 1;
            // Exact token-set but different word order: e.g. Dissidia Final Fantasy.
            // Do not drop a number or edition marker to force an identity.
            if(local.Count>=3 && local.Count==name.Count &&
                local.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(
                    name.OrderBy(x=>x,StringComparer.Ordinal),StringComparer.Ordinal) &&
                !Edition.IsMatch(requested) && !Edition.IsMatch(official))return 2;
            // Incomplete titles are common in ROM libraries. A proper multiword
            // title can identify a single platform-specific subtitle release.
            // A bare series name is not sufficient (Mario Kart / Final Fantasy).
            if(StartsWith(name,local) && !Edition.IsMatch(requested) &&
                !Edition.IsMatch(official) && !BroadFranchises.Contains(String.Join(" ",local)) &&
                (local.Count>=4 || (local.Count>=3 && local.Any(x=>x.All(Char.IsDigit))) ||
                 (local.Count>=2 && requested.Length>=9 &&
                  official.IndexOf(':')>=0 && local.Any(x=>x.Length>=5))))return 1;
            if(FlexibleNumberedPrefix(name,local) &&
                !Edition.IsMatch(requested) && !Edition.IsMatch(official))return 1;
            if(NearSameFullTitle(local,name))return 2;
            return 0;
        }
        private static bool HasConflictingQualifier(string requested,string official,int kind)
        {
            if(kind==3)return false; // exact explicit editions match.
            var local=Edition.Matches(requested??"").Cast<Match>().Select(m=>m.Value.ToLowerInvariant()).ToList();
            var remote=Edition.Matches(official??"").Cast<Match>().Select(m=>m.Value.ToLowerInvariant()).ToList();
            return local.Count>0 || remote.Count>0;
        }
        private static int PlatformYear(CatalogGame game,long platform)
        {
            var years=(game.ReleaseDates??new List<CatalogRelease>())
                .Where(r=>r!=null && r.PlatformId==platform && r.Year>=1970)
                .Select(r=>r.Year).ToList();
            // An all-platform first release is not independent corroboration
            // for a later port. Use only the target-platform's dated release.
            return years.Count>0?years.Min():0;
        }
        // Exact explicit editions can be legitimate releases. Parent/version
        // relationships do not disqualify a full-name match; they disqualify
        // *inferred* equivalence between a base game and an unknown edition.
        private static bool IsPlayable(CatalogGame game,int titleKind,string localTitle)
        {
            // Unknown provider type is not evidence that this record is a
            // playable game. Editions and updates can otherwise slip through.
            if(game==null || !game.GameTypeId.HasValue)return false;
            var type=game.GameTypeId;
            if(type!=0 && type!=3 && type!=4 && type!=6 && type!=8 && type!=9 &&
               type!=10 && type!=11)return false;
            // Standalone expansions / named episodes require a complete title.
            if((type==4 || type==6) && titleKind!=3)return false;
            bool isVariant=game.ParentGameId.GetValueOrDefault()>0 ||
                game.VersionParentId.GetValueOrDefault()>0 ||
                !String.IsNullOrWhiteSpace(game.VersionTitle) || type==10;
            if(isVariant && titleKind!=3)return false;
            if(!String.IsNullOrWhiteSpace(game.VersionTitle))
            {
                var edition=Tokens(game.VersionTitle);
                var local=Tokens(localTitle);
                if(edition.Count==0 || !edition.All(local.Contains))return false;
            }
            return true;
        }
        private static Ranked Rank(CatalogInput input,CatalogGame game,long platform)
        {
            if(game==null || game.Id<=0 || game.PlatformIds==null ||
                !game.PlatformIds.Contains(platform))return null;
            var requested=CatalogCore.QueryTitle(input.Name,input.Platform);
            int kind=TitleKind(requested,game.Name,game.Aliases);
            if(kind==0 || HasConflictingQualifier(requested,game.Name,kind) ||
                !IsPlayable(game,kind,requested))return null;
            int points=kind==3?130:kind==2?105:92;
            int year=PlatformYear(game,platform);
            if(input.LocalReleaseYear>=1970 && year>=1970)
            {
                int diff=Math.Abs(year-input.LocalReleaseYear);
                points+=diff<=1?18:diff==2?6:diff<=4?-10:-25;
            }
            if(kind==1 && game.GameTypeId==3)points-=17; // compound collection, if a standalone exists.
            if(kind==3 && game.GameTypeId==3)points+=2; // a named bundle may be the correct release.
            if(kind==3 && game.GameTypeId==0)points+=3;
            // Provider popularity only breaks *minor* ordering ties. It is not
            // evidence of a particular local copy's provenance.
            if(game.RatingCount>=100)points+=2;
            return new Ranked{Game=game,Kind=kind,Points=points,Year=year};
        }
        internal static string BuildPrefixQuery(string title,string platform)
        {
            long id=CatalogCore.PlatformId(platform);
            if(id<=0 || String.IsNullOrWhiteSpace(title) || title.Length>100 ||
                !Regex.IsMatch(title,@"^[\p{L}\p{N} \-:'.&/+!?(),]+$"))return "";
            title=Regex.Replace(title.Trim(),@"\s+"," ");
            if(title.Length<4)return "";
            return "fields id,name,summary,platforms.id,platforms.name,alternative_names.name,"+
                "cover.image_id,screenshots.image_id,first_release_date,game_type,version_parent,"+
                "parent_game,version_title,total_rating_count,release_dates.date,release_dates.platform.id; "+
                "where platforms = "+id+" & name ~ \""+title+"\"*; limit "+MaxLookupResults+";";
        }
        internal static List<CatalogGame> MergeCandidates(IEnumerable<CatalogGame> candidates)
        {
            // Prefer the fullest representation of an ID when search and exact
            // lookups differ in field coverage. Do not merge contradictory types
            // or platform lists into a synthetic, potentially false identity.
            return (candidates??Enumerable.Empty<CatalogGame>())
                .Where(g=>g!=null && g.Id>0).GroupBy(g=>g.Id)
                .Select(group=>group.OrderByDescending(g=>
                    (g.GameTypeId.HasValue?8:0)+
                    (g.PlatformIds==null?0:g.PlatformIds.Count)+
                    (g.ReleaseDates==null?0:g.ReleaseDates.Count)+
                    (g.Aliases==null?0:g.Aliases.Count)+
                    (g.FirstReleaseYear>0?1:0)).First()).ToList();
        }
        internal static CatalogMatch Resolve(CatalogInput input,IEnumerable<CatalogGame> candidates,
            CatalogMatch preliminary,bool complete)
        {
            if(!complete || input==null || preliminary==null ||
                CatalogCore.PlatformId(input.Platform)<=0)return preliminary;
            var all=MergeCandidates(candidates);
            long platform=CatalogCore.PlatformId(input.Platform);
            var ranked=all.Select(g=>Rank(input,g,platform)).Where(x=>x!=null)
                .OrderByDescending(x=>x.Points).ThenBy(x=>x.Game.Id).ToList();
            if(ranked.Count==0)return preliminary;
            var winner=ranked[0];
            var rival=ranked.Skip(1).FirstOrDefault();
            // An exceptionally well-established catalogue entry can be a useful
            // assisted choice among *exactly the same* official game titles. It
            // is not a calibrated 80% probability: require a very large vote
            // disparity and substantive record context; keep close calls in review.
            bool dominantCatalogConsensus=false;
            var exacts=ranked.Where(x=>x.Kind==3 &&
                EqualTokens(Tokens(x.Game.Name),Tokens(winner.Game.Name))).ToList();
            if(input.LocalReleaseYear<1970 && exacts.Count>=2 &&
                exacts.All(x=>x.Game.GameTypeId==0 && x.Year>=1970))
            {
                var popularity=exacts.OrderByDescending(x=>x.Game.RatingCount).ToList();
                var most=popularity[0];var second=popularity[1];
                bool distinctive=Tokens(input.Name).Count>=2 ||
                    (most.Game.RatingCount>=500 &&
                     most.Game.RatingCount>=100*Math.Max(1,second.Game.RatingCount));
                if(distinctive && Math.Abs(most.Year-second.Year)<10 &&
                    most.Game.RatingCount>=150 && second.Game.RatingCount<=10 &&
                    most.Game.RatingCount>=25*Math.Max(1,second.Game.RatingCount) &&
                    !String.IsNullOrWhiteSpace(most.Game.Summary) &&
                    most.Game.Summary.Trim().Length>=80 &&
                    !BroadFranchises.Contains(String.Join(" ",Tokens(input.Name))))
                {
                    winner=most;
                    ranked.Remove(most);ranked.Insert(0,most);
                    rival=ranked.Skip(1).FirstOrDefault();
                    dominantCatalogConsensus=true;
                }
            }
            int margin=rival==null?100:winner.Points-rival.Points;
            if(!dominantCatalogConsensus)
            {
                if(winner.Kind==1 && margin<14)return preliminary;
                if(winner.Kind==2 && margin<18)return preliminary;
                if(winner.Kind==3 && margin<10)return preliminary;
                // Two genuine full-title homonyms still need independent year
                // evidence unless the catalogue dominance rule is exceptional.
                if(rival!=null && winner.Kind==3 && rival.Kind==3 &&
                    (input.LocalReleaseYear<1970 || Math.Abs(winner.Year-input.LocalReleaseYear)>1 ||
                        rival.Year<1970 || Math.Abs(rival.Year-input.LocalReleaseYear)<3))return preliminary;
            }
            if(winner.Kind==1 && winner.Game.GameTypeId==3)return preliminary;
            // Prefix-based identity needs dated target-platform evidence.
            if(winner.Kind==1 && (!winner.Game.GameTypeId.HasValue ||
                winner.Year<1970))return preliminary;
            // Reuse the exact ID's full search metadata. A probe-only record is
            // never used as a metadata source.
            var resolved=CatalogCore.Choose(input,new[]{winner.Game});
            if(resolved.Game==null || resolved.Game.Id!=winner.Game.Id ||
                !LiveIdentityCompatible(input,winner.Game))return preliminary;
            resolved.SmartAutoEvidence=true;
            resolved.MatchStatus="Strong candidate";
            resolved.StructuredPlatformEvidence=true;
            resolved.PlatformMatches=true;
            resolved.CandidatesFound=all.Count;
            resolved.Alternatives=ranked.Skip(1).Take(3).Select(x=>x.Game).ToList();
            resolved.Reason+="; smart-auto verified: catalog candidate #"+winner.Game.Id+
                ", ordinal/edition and platform checked, "+
                (winner.Kind==3?"official full title":winner.Kind==2?"related alias or near-identical full title":"unique franchise/ordinal prefix")+
                ", lead "+margin+" ranking points (NOT probability), "+
                (dominantCatalogConsensus?"exceptional provider popularity consensus (not independent local proof), ":"")+
                "bounded exact/prefix rival queries complete; only missing fields eligible; live ID preflight required";
            return resolved;
        }
        internal static bool LiveIdentityCompatible(CatalogInput input,CatalogGame candidate)
        {
            long id=CatalogCore.PlatformId(input==null?null:input.Platform);
            if(id<=0 || candidate==null || candidate.Id<=0 || candidate.PlatformIds==null ||
                !candidate.PlatformIds.Contains(id))return false;
            var requested=CatalogCore.QueryTitle(input.Name,input.Platform);
            int kind=TitleKind(requested,candidate.Name,candidate.Aliases);
            return kind>0 && candidate.GameTypeId.HasValue &&
                IsPlayable(candidate,kind,requested) &&
                !HasConflictingQualifier(requested,candidate.Name,kind);
        }
    }
}
