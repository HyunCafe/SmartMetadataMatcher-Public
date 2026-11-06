using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Ephemeral draft: no Playnite mutations and no durable approval until the
    // user accepts the final import plan. All controls are explicit opt-ins/outs.
    internal sealed class CatalogImportDraft
    {
        internal Guid GameId;
        internal long IgdbId;
        internal string ExpectedName, ExpectedPlatform, ExpectedCatalogTitle;
        internal bool CustomTitleEnabled, CustomDescriptionEnabled;
        internal string CustomTitle, CustomDescription;
        internal bool Description=true, Cover=true, Icon=true, Background=true;
        internal bool Release=true, Genres=true, Developers=true, Publishers=true, Series=true;

        internal CatalogImportDraft Copy()
        {
            return (CatalogImportDraft)MemberwiseClone(); // strings and value types only
        }
        internal bool Matches(Guid game,string name,string platform,long id,string catalog)
        {
            return GameId==game && IgdbId==id && id>0 &&
                String.Equals(ExpectedName,name,StringComparison.Ordinal) &&
                String.Equals(ExpectedPlatform,platform,StringComparison.Ordinal) &&
                String.Equals(ExpectedCatalogTitle,catalog,StringComparison.Ordinal);
        }
        internal bool Customized()
        {
            return CustomTitleEnabled || CustomDescriptionEnabled ||
                !Description || !Cover || !Icon || !Background || !Release ||
                !Genres || !Developers || !Publishers || !Series;
        }
        internal static bool SafeDescription(string text)
        {
            return !String.IsNullOrWhiteSpace(text) && text.Length<=12000 &&
                text.IndexOf('\0')<0;
        }
        internal static string ResolveTitle(string original,string catalog,Guid gameId,
            string platform,IEnumerable<Tuple<Guid,string,string>> library,bool autoRename,
            CatalogImportDraft draft)
        {
            if(draft!=null && draft.CustomTitleEnabled)
            {
                var value=(draft.CustomTitle??"").Trim();
                if(!EnrichmentPlan.IsSafeTitle(value))return null;
                // Even manual titles may not silently collide with another game.
                if((library??Enumerable.Empty<Tuple<Guid,string,string>>()).Any(x=>
                    x.Item1!=gameId && EnrichmentPlan.IdentityKey(x.Item2)==EnrichmentPlan.IdentityKey(value)))
                    return null;
                return value;
            }
            if(!autoRename)return original;
            var automatic=EnrichmentPlan.SuggestedUniqueDisplayTitle(
                original,catalog,gameId,platform,library);
            return String.IsNullOrWhiteSpace(automatic)?original:automatic;
        }
        internal static bool Valid(CatalogImportDraft draft)
        {
            return draft!=null && draft.GameId!=Guid.Empty && draft.IgdbId>0 &&
                !String.IsNullOrWhiteSpace(draft.ExpectedCatalogTitle) &&
                (!draft.CustomTitleEnabled || EnrichmentPlan.IsSafeTitle((draft.CustomTitle??"").Trim())) &&
                (!draft.CustomDescriptionEnabled || SafeDescription(draft.CustomDescription));
        }
    }
}
