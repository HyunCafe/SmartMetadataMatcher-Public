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
        Check(CatalogEnrichmentNeeds.Describe(8)=="Icon","single missing field identified");
        Check(CatalogEnrichmentNeeds.Describe(0)=="None","no missing fields visible");
        Check(CatalogEnrichmentNeeds.Describe(511).Contains("Series"),"all nine fields exported");
        Check(CatalogEnrichmentNeeds.CandidateOverlap(1|2|4|8|16,
            "description cover screenshot")=="Description; Cover; Background; Icon",
            "only fields evidenced in scan presented as potential fills");
        Check(CatalogEnrichmentNeeds.CandidateOverlap(16|32|64|128|256,
            "description cover screenshot")=="None",
            "scan must not promise release date, genres or companies");
        Check(CatalogEnrichmentNeeds.CandidateOverlap(1|2|4,"")=="None",
            "no proposed fill without scan evidence");
        bool rejected=false;
        try {CatalogEnrichmentNeeds.Count(1024);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"unexpected mask bits rejected");
        Console.WriteLine("PASS: "+checks+" supported-field discovery and shared-stage recovery checks.");
    }
}
