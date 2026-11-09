using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // Pure decisions. No Playnite SDK or IO. A match score never authorizes writes.
    internal static class EnrichmentPlan
    {
        // Existing Strong label is a prerequisite for a supervised batch, never
        // proof of identity or permission for unattended library modifications.
        internal static bool HasUnresolvedJournalEntry(IEnumerable<EnrichmentEntry> entries)
        {
            // Any incomplete write is a global block for further enrichment,
            // even if the next game differs. Recovery/undo is required first.
            return entries!=null && entries.Any(x=>x!=null && !x.Committed);
        }
        // Stack undo may only be invoked for a contiguous newest-first prefix.
        // Selection order and UI sort order must not change the journal's order.
        internal static bool IsLatestUndoPrefix(IEnumerable<Guid> newestFirst, IEnumerable<Guid> chosen)
        {
            var history=(newestFirst??Enumerable.Empty<Guid>()).ToList();
            var selected=(chosen??Enumerable.Empty<Guid>()).ToList();
            if(selected.Count==0 || selected.Any(id=>id==Guid.Empty) ||
               selected.Distinct().Count()!=selected.Count || selected.Count>history.Count)
                return false;
            return history.Take(selected.Count).OrderBy(id=>id)
                .SequenceEqual(selected.OrderBy(id=>id));
        }
        // Calculate only the maximal newest-first prefix of selected *active*
        // operations. Historical Undone entries are absent from newestFirstActive.
        // This never grants permission to undo arbitrary older records.
        internal static List<Guid> LatestSelectedUndoPrefix(IEnumerable<Guid> newestFirstActive,
            IEnumerable<Guid> selectedApplied)
        {
            var wanted=new HashSet<Guid>((selectedApplied??Enumerable.Empty<Guid>())
                .Where(id=>id!=Guid.Empty));
            var prefix=new List<Guid>();
            foreach(var id in newestFirstActive??Enumerable.Empty<Guid>())
            {
                if(id==Guid.Empty || !wanted.Contains(id))break;
                prefix.Add(id);
            }
            return prefix;
        }
        // Pure result gate. A zero-field result is safe to SKIP only when no
        // media upload and no globally shared Playnite entity creation was
        // attempted. Uncertain cases remain hard failures with an open stage.
        internal static bool HasChanges(EnrichmentEntry e)
        {
            if(e==null)throw new ArgumentNullException("e");
            return e.ChangedName || e.ChangedSorting || e.ChangedDescription ||
                e.ChangedCover || e.ChangedIcon || e.ChangedBackground ||
                e.ChangedRelease || e.ChangedGenres || e.ChangedDevelopers ||
                e.ChangedPublishers || e.ChangedSeries;
        }
        internal static bool CleanNoChange(EnrichmentEntry e,int importedMediaCount,
            bool mediaUploadUncertain,bool sharedReferenceWriteAttempted)
        {
            if(importedMediaCount<0)throw new ArgumentOutOfRangeException("importedMediaCount");
            return !HasChanges(e) && importedMediaCount==0 &&
                CatalogEnrichmentNeeds.MayAbandonCleanStage(true,
                    mediaUploadUncertain,sharedReferenceWriteAttempted);
        }

        internal static bool BatchEligible(string status,bool completed,string catalogId)
        {
            long id;
            return !completed && String.Equals(status,"Strong candidate",StringComparison.Ordinal) &&
                Int64.TryParse(catalogId,out id) && id>0;
        }
        internal static string IdentityKey(string value)
        {
            var decomposed=(value ?? "").Normalize(NormalizationForm.FormD);
            var b=new StringBuilder();
            foreach(char c in decomposed)
            {
                if(CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark) continue;
                if(c=='+') b.Append("plus");
                else if(Char.IsLetterOrDigit(c)) b.Append(Char.ToLowerInvariant(c));
            }
            return b.ToString();
        }
        // One approved Playnite display title, not a ROM filename rewrite.
        // Preserve a clean local title when IGDB only adds the generic 'Version'
        // suffix; use the catalog spelling for substantive abbreviation fixes.
        internal static string SuggestedDisplayTitle(string original,string catalog)
        {
            if(!IsSafeTitle(catalog)) return "";
            var candidate=(original??"").Trim();
            var match=System.Text.RegularExpressions.Regex.Match(candidate,
                @"^(?:0[1-9]|[1-9][0-9])[\s._-]+(?<name>[\p{L}][^\r\n]{2,})$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            if(!match.Success) return catalog.Trim();
            var cleaned=match.Groups["name"].Value.Trim();
            var cleanKey=IdentityKey(cleaned);
            var catalogKey=IdentityKey(catalog);
            if(catalogKey==cleanKey || catalogKey==cleanKey+"version") return cleaned;
            return catalog.Trim();
        }
        // Pure, user-reviewable Playnite display-name proposal. When the canonical
        // title already exists in the library (including another console),
        // append a short platform tag to distinguish the Playnite display name.
        // No ROM paths, game IDs, launches or files are touched here.
        internal static string PlatformDisplaySuffix(string platform)
        {
            switch(CatalogCore.PlatformKey(platform))
            {
                case "gba": return "GBA";
                case "gbc": return "GBC";
                case "gb": return "GB";
                case "nds": return "NDS";
                case "3ds": return "3DS";
                case "psp": return "PSP";
                case "ps1": return "PS1";
                case "ps2": return "PS2";
                case "pc": return "PC";
                case "switch": return "Switch";
                default: return "";
            }
        }
        internal static string SuggestedUniqueDisplayTitle(string original,string catalog,Guid gameId,
            string platform,IEnumerable<Tuple<Guid,string,string>> library)
        {
            var clean=SuggestedDisplayTitle(original,catalog);
            if(!IsSafeTitle(clean))return "";
            var all=(library??Enumerable.Empty<Tuple<Guid,string,string>>()).ToList();
            // This is a display-name choice, not a game-identity decision:
            // matching title on any console should be distinguished visibly.
            if(!all.Any(x=>x.Item1!=gameId && IdentityKey(x.Item2)==IdentityKey(clean)))
                return clean;
            var suffix=PlatformDisplaySuffix(platform);
            if(suffix.Length==0)return ""; // Unknown platform: require explicit manual decision.
            var tagged=clean+" ("+suffix+")";
            if(!IsSafeTitle(tagged) || all.Any(x=>x.Item1!=gameId &&
                IdentityKey(x.Item2)==IdentityKey(tagged)))
                return ""; // Still ambiguous; never invent #2/#3 labels.
            return tagged;
        }
        internal static bool IsSafeTitle(string value)
        {
            return !String.IsNullOrWhiteSpace(value) && value.Trim().Length>=1 && value.Trim().Length<=160 &&
                value.IndexOfAny(new[]{'\r','\n','\0'})<0;
        }
        internal static bool HasCollision(string proposed, Guid gameId, string platform,
            IEnumerable<Tuple<Guid,string,string>> games)
        {
            var key=IdentityKey(proposed);
            var plat=CatalogCore.PlatformKey(platform);
            return games.Any(x=>x.Item1!=gameId && IdentityKey(x.Item2)==key &&
                CatalogCore.PlatformKey(x.Item3)==plat);
        }
        internal static bool MaySetSorting(string sorting, string originalName)
        {
            // Deliberately customized sort names must be preserved.
            return String.IsNullOrWhiteSpace(sorting) ||
                String.Equals(sorting,originalName,StringComparison.Ordinal);
        }
        internal static bool IsExpected(string current, string expected)
        {
            return String.Equals(current ?? "",expected ?? "",StringComparison.Ordinal);
        }
        internal static bool IsFullyApplied(EnrichmentEntry e,string name,string sorting,string desc,
            string cover,string icon,string background)
        {
            return e!=null &&
                (!e.ChangedName || IsExpected(name,e.AfterName)) &&
                (!e.ChangedSorting || IsExpected(sorting,e.AfterSorting)) &&
                (!e.ChangedDescription || IsExpected(desc,e.AfterDescription)) &&
                (!e.ChangedCover || IsExpected(cover,e.AfterCover)) &&
                (!e.ChangedIcon || IsExpected(icon,e.AfterIcon)) &&
                (!e.ChangedBackground || IsExpected(background,e.AfterBackground));
        }
        internal static bool SameIds(IList<Guid> left,IList<Guid> right)
        {
            var a=(left??new List<Guid>()).OrderBy(x=>x).ToList();
            var b=(right??new List<Guid>()).OrderBy(x=>x).ToList();
            return a.SequenceEqual(b);
        }
        internal static bool ExtraFieldsApplied(EnrichmentEntry e,string release,
            IList<Guid> genres,IList<Guid> developers,IList<Guid> publishers,IList<Guid> series)
        {
            return e!=null &&
                (!e.ChangedRelease || IsExpected(release,e.AfterRelease)) &&
                (!e.ChangedGenres || SameIds(genres,e.AfterGenres)) &&
                (!e.ChangedDevelopers || SameIds(developers,e.AfterDevelopers)) &&
                (!e.ChangedPublishers || SameIds(publishers,e.AfterPublishers)) &&
                (!e.ChangedSeries || SameIds(series,e.AfterSeries));
        }
        internal static bool ExtraFieldsUndoSafe(EnrichmentEntry e,string release,
            IList<Guid> genres,IList<Guid> developers,IList<Guid> publishers,IList<Guid> series)
        {
            return e!=null &&
                UndoFieldSafe(e.ChangedRelease,release,e.BeforeRelease,e.AfterRelease) &&
                (!e.ChangedGenres || SameIds(genres,e.BeforeGenres) || SameIds(genres,e.AfterGenres)) &&
                (!e.ChangedDevelopers || SameIds(developers,e.BeforeDevelopers) || SameIds(developers,e.AfterDevelopers)) &&
                (!e.ChangedPublishers || SameIds(publishers,e.BeforePublishers) || SameIds(publishers,e.AfterPublishers)) &&
                (!e.ChangedSeries || SameIds(series,e.BeforeSeries) || SameIds(series,e.AfterSeries));
        }
        // Reject any field changed after our update; independent fields may already
        // equal their original value after a partial interruption or manual reversal.
        internal static bool UndoFieldSafe(bool changed, string live, string before, string after)
        {
            return !changed || IsExpected(live,before) || IsExpected(live,after);
        }
        internal static bool CanUndo(EnrichmentEntry e,string name,string sorting,string desc,
            string cover,string icon,string background)
        {
            return e!=null &&
                UndoFieldSafe(e.ChangedName,name,e.BeforeName,e.AfterName) &&
                UndoFieldSafe(e.ChangedSorting,sorting,e.BeforeSorting,e.AfterSorting) &&
                UndoFieldSafe(e.ChangedDescription,desc,e.BeforeDescription,e.AfterDescription) &&
                UndoFieldSafe(e.ChangedCover,cover,e.BeforeCover,e.AfterCover) &&
                UndoFieldSafe(e.ChangedIcon,icon,e.BeforeIcon,e.AfterIcon) &&
                UndoFieldSafe(e.ChangedBackground,background,e.BeforeBackground,e.AfterBackground);
        }
    }
}
