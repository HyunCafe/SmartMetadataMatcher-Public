using System;

namespace SmartMetadataMatcher
{
    // Same missing-field policy for discovery, rescan, draft refresh and scan cache.
    // This class is independent of Playnite/WPF to permit offline regression tests.
    [Flags]
    internal enum CatalogMissingField
    {
        None=0,
        Description=1, Cover=2, Background=4, Icon=8,
        Release=16, Genres=32, Developers=64, Publishers=128, Series=256
    }

    internal static class CatalogEnrichmentNeeds
    {
        internal const int AllMask=511;
        internal static int Mask(bool description,bool cover,bool background,bool icon,
            bool release,bool genres,bool developers,bool publishers,bool series)
        {
            int value=0;
            if(description)value|=(int)CatalogMissingField.Description;
            if(cover)value|=(int)CatalogMissingField.Cover;
            if(background)value|=(int)CatalogMissingField.Background;
            if(icon)value|=(int)CatalogMissingField.Icon;
            if(release)value|=(int)CatalogMissingField.Release;
            if(genres)value|=(int)CatalogMissingField.Genres;
            if(developers)value|=(int)CatalogMissingField.Developers;
            if(publishers)value|=(int)CatalogMissingField.Publishers;
            if(series)value|=(int)CatalogMissingField.Series;
            return value;
        }
        internal static int Count(int mask)
        {
            if(mask<0 || (mask & ~AllMask)!=0)
                throw new ArgumentOutOfRangeException("mask");
            int count=0;
            while(mask!=0){count+=mask&1;mask>>=1;}
            return count;
        }
        internal static string Describe(int mask)
        {
            Count(mask); // Reject unexpected bits instead of exporting misleading fields.
            var names=new System.Collections.Generic.List<string>();
            if((mask & 1)!=0)names.Add("Description");
            if((mask & 2)!=0)names.Add("Cover");
            if((mask & 4)!=0)names.Add("Background");
            if((mask & 8)!=0)names.Add("Icon");
            if((mask & 16)!=0)names.Add("Release date");
            if((mask & 32)!=0)names.Add("Genres");
            if((mask & 64)!=0)names.Add("Developers");
            if((mask & 128)!=0)names.Add("Publishers");
            if((mask & 256)!=0)names.Add("Series");
            return names.Count==0?"None":String.Join("; ",names);
        }
        internal static string CandidateOverlap(int mask,string availableMetadata)
        {
            Count(mask);
            // Current search only reports summary, cover and screenshot.
            // This is a potential opportunity, NOT a guaranteed writer result.
            var tokens=new System.Collections.Generic.HashSet<string>(
                (availableMetadata??"").Split(new[]{' ', ',', ';', '|'},
                    StringSplitOptions.RemoveEmptyEntries),StringComparer.OrdinalIgnoreCase);
            int overlap=0;
            if(tokens.Contains("description"))overlap|=mask & 1;
            if(tokens.Contains("cover"))overlap|=mask & (2|8); // IGDB cover can supply icon.
            if(tokens.Contains("screenshot"))overlap|=mask & 4;
            return Describe(overlap);
        }
        internal static int PrimaryCount(int mask)
        {
            return Count(mask & ((int)CatalogMissingField.Description |
                (int)CatalogMissingField.Cover | (int)CatalogMissingField.Background));
        }
        internal static bool MayAbandonCleanStage(bool mediaCleanupSucceeded,
            bool mediaUploadUncertain,bool sharedReferenceWriteAttempted)
        {
            // Even if Playnite throws before an Add() returns an ID, its global
            // shared object may have been created. Never certify that stage clean.
            return mediaCleanupSucceeded && !mediaUploadUncertain && !sharedReferenceWriteAttempted;
        }
    }
}
