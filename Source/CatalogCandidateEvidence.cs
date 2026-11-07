using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // A bounded, read-only investigation of IGDB candidates. This class does not
    // assign a correctness probability, change Choose(), or grant write approval.
    internal static class CatalogCandidateEvidence
    {
        internal const int MaxIds = 12;
        private static bool OnPlatform(CatalogInput input, CatalogGame game)
        {
            long id = CatalogCore.PlatformId(input == null ? null : input.Platform);
            return id > 0 && game != null && game.PlatformIds != null && game.PlatformIds.Contains(id);
        }
        internal static List<long> SelectIds(CatalogInput input, IEnumerable<CatalogGame> candidates,
            long selectedId)
        {
            // Favor the existing selected candidate and same-platform rivals.
            // Other-platform rivals are still represented when room is available.
            return (candidates ?? Enumerable.Empty<CatalogGame>())
                .Where(x => x != null && x.Id > 0)
                .GroupBy(x => x.Id).Select(x => x.First())
                .OrderByDescending(x => x.Id == selectedId)
                .ThenByDescending(x => OnPlatform(input, x))
                .ThenByDescending(x => CatalogCore.Choose(input, new[] { x }).Score)
                .ThenBy(x => x.Id).Take(MaxIds).Select(x => x.Id).ToList();
        }
        internal static string Describe(CatalogInput input, IEnumerable<CatalogGame> raw,
            IList<long> requestedIds, IEnumerable<CatalogGame> inspected)
        {
            var original=(raw ?? Enumerable.Empty<CatalogGame>()).Where(x => x!=null && x.Id>0)
                .GroupBy(x=>x.Id).Select(x=>x.First()).ToList();
            var found=(inspected ?? Enumerable.Empty<CatalogGame>()).Where(x=>x!=null && x.Id>0)
                .GroupBy(x=>x.Id).Select(x=>x.First()).ToList();
            var requested=(requestedIds ?? new List<long>()).Distinct().ToList();
            long platform=CatalogCore.PlatformId(input==null?null:input.Platform);
            int onPlatform=found.Count(g=>OnPlatform(input,g));
            bool incomplete=requested.Count<original.Count ||
                found.Count!=requested.Count || platform<=0;
            var samples=new List<string>();
            foreach(var game in found.Where(g=>OnPlatform(input,g)).Take(3))
            {
                var details=new List<string>();
                if(game.GameTypeId.HasValue) details.Add("type #"+game.GameTypeId.Value.ToString(CultureInfo.InvariantCulture));
                if(game.VersionParentId.HasValue && game.VersionParentId.Value>0)
                    details.Add("version of #"+game.VersionParentId.Value.ToString(CultureInfo.InvariantCulture));
                if(game.ParentGameId.HasValue && game.ParentGameId.Value>0)
                    details.Add("parent #"+game.ParentGameId.Value.ToString(CultureInfo.InvariantCulture));
                var years=(game.ReleaseDates??new List<CatalogRelease>())
                    .Where(d=>d.PlatformId==platform && d.Year>=1970)
                    .Select(d=>d.Year).Distinct().OrderBy(y=>y).Take(2).ToList();
                if(years.Count>0)details.Add("platform year "+String.Join("/",years));
                samples.Add("#"+game.Id+" "+((game.Name??"").Length>45?(game.Name??"").Substring(0,45):game.Name)+" ["+(details.Count==0?"type/release unknown":String.Join(", ",details))+"]");
            }
            // Never claim exhaustive coverage: IGDB's initial search is capped,
            // even if every ID from that response was inspected successfully.
            return "IGDB read-only evidence: "+found.Count+"/"+requested.Count+
                " requested IDs (sample "+String.Join(",",requested)+"), "+onPlatform+" same-platform"+
                (incomplete?" (partial/unknown coverage)":" (search coverage unverified)")+
                (samples.Count==0?"":"; "+String.Join("; ",samples))+
                "; not an automatic identity approval";
        }
    }
}
