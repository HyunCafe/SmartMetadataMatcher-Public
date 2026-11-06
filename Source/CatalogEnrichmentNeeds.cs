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
