using System;
using System.Collections.Generic;

namespace SmartMetadataMatcher
{
    // Read-only, conservative fields visible before live IGDB downloads.
    // Extra release/genre/company/series fields require live catalog preflight.
    internal static class CatalogImportIntent
    {
        internal static string Describe(bool titleChanges,bool missingDescription,
            bool missingCover,bool missingIcon,bool missingBackground,
            bool hasCatalogSummary,bool hasCoverId,bool hasScreenshotId)
        {
            var fields=new List<string>();
            if(titleChanges)fields.Add("Title");
            if(missingDescription && hasCatalogSummary)fields.Add("Description");
            if(missingCover && hasCoverId)fields.Add("Cover");
            if(missingIcon && hasCoverId)fields.Add("Icon");
            if(missingBackground && hasScreenshotId)fields.Add("Background");
            return fields.Count==0?"No previewable fields; live check needed":
                String.Join(", ",fields);
        }
    }
}
