using System;
using SmartMetadataMatcher;

internal static class CatalogEnrichmentNeedsTests
{
    private static int checks;
    private static void Check(bool yes,string label)
    {
        checks++;
        if(!yes)throw new Exception("FAIL: supported enrichment fields: "+label);
    }
    internal static void Run()
    {
        var all=CatalogEnrichmentNeeds.Mask(true,true,true,true,true,true,true,true,true);
        Check(all==CatalogEnrichmentNeeds.AllMask,"full supported mask");
        Check(CatalogEnrichmentNeeds.Count(all)==9,"all nine missing fields eligible");
        Check(CatalogEnrichmentNeeds.Count(0)==0,"fully enriched is not queued");
        Check(CatalogEnrichmentNeeds.PrimaryCount(all)==3,"legacy primary count");
        Check(CatalogEnrichmentNeeds.PrimaryCount(0)==0,"complete legacy fields");
        for(int i=0;i<9;i++)
        {
            int mask=1<<i;
            Check(CatalogEnrichmentNeeds.Count(mask)==1,"single missing field "+i);
            Check(mask!=0,"single field discoverable "+i);
        }
        Check(CatalogEnrichmentNeeds.PrimaryCount(8)==0,"icon-only skipped by legacy but now discoverable");
        Check(CatalogEnrichmentNeeds.PrimaryCount(16)==0,"release-only skipped by legacy but now discoverable");
        Check(!CatalogEnrichmentNeeds.MayAbandonCleanStage(true,false,true),"global metadata Add cannot be silently abandoned");
        Check(!CatalogEnrichmentNeeds.MayAbandonCleanStage(true,true,false),"uncertain artwork cannot be abandoned");
        Check(!CatalogEnrichmentNeeds.MayAbandonCleanStage(false,false,false),"failed artwork cleanup cannot be abandoned");
        Check(CatalogEnrichmentNeeds.MayAbandonCleanStage(true,false,false),"clean no-write stage can be abandoned");
        Check(!CatalogEnrichmentNeeds.MayAbandonCleanStage(false,true,true),"combined failure stays unresolved");
        bool rejected=false;
        try {CatalogEnrichmentNeeds.Count(1024);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"unexpected mask bits rejected");
        Console.WriteLine("PASS: "+checks+" supported-field discovery and shared-stage recovery checks.");
    }
}
