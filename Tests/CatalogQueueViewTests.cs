using System;
using SmartMetadataMatcher;

internal static class CatalogQueueViewTests
{
    internal static void Run()
    {
        int checks=0;
        Action<bool,string> verify=(yes,label)=>{
            checks++;
            if(!yes)throw new Exception("Catalog queue UI: "+label);
        };
        verify(CatalogQueueView.RequiresCachedRefresh("Strong candidate",false,false,true,false,false),
            "cached strong result needs fresh IGDB verification");
        verify(!CatalogQueueView.RequiresCachedRefresh("Strong candidate",false,false,false,true,false),
            "fresh strong match must not be refreshed again");
        verify(!CatalogQueueView.RequiresCachedRefresh("Strong candidate",true,false,true,false,false),
            "completed game must not be refreshed");
        verify(!CatalogQueueView.RequiresCachedRefresh("Strong candidate",false,true,true,false,false),
            "ignored game must not be refreshed");
        verify(!CatalogQueueView.RequiresCachedRefresh("Review",false,false,true,false,false),
            "manual review candidate is not marked Refresh required");
        verify(!CatalogQueueView.RequiresCachedRefresh("Unmatched",false,false,true,false,false),
            "unmatched cached game is not marked Refresh required");
        verify(!CatalogQueueView.RequiresCachedRefresh("Strong candidate",false,false,true,false,true),
            "user-confirmed game must not lose its confirmed decision");
        verify(!CatalogQueueView.RequiresCachedRefresh("No changes available",false,false,true,false,false),
            "no-change result is not refresh-required");
        verify(EnrichmentJobPlanner.DashboardStatus("Strong candidate",false,false,true,false,false)==
            "Refresh required","UI classification matches cached-refresh action");
        var statuses=new[]{"Strong candidate","Review","Strong candidate","Strong candidate"};
        var cached=new[]{true,true,false,true};
        int required=0;
        for(int i=0;i<statuses.Length;i++)
            if(CatalogQueueView.RequiresCachedRefresh(statuses[i],i==3,false,cached[i],false,false))
                required++;
        verify(required==1,"batch refresh selects only cached unresolved strong matches");
        verify(CatalogQueueView.Bucket("Strong candidate",false,false,true)==0,"strong candidate");
        verify(CatalogQueueView.Bucket("Confirmed",false,false,true)==0,"user confirmed");
        verify(CatalogQueueView.Bucket("Review",false,false,false)==1,"review");
        verify(CatalogQueueView.Bucket("Unmatched",false,false,false)==2,"unmatched");
        verify(CatalogQueueView.Bucket("No changes available",false,false,false)==3,"no-change separate");
        verify(CatalogQueueView.Bucket("Completed",true,false,false)==4,"completed");
        verify(CatalogQueueView.Bucket("Ignored",false,true,false)==5,"ignored");
        verify(CatalogQueueView.Bucket("No changes available",true,false,false)==4,"completion precedence");
        verify(CatalogQueueView.Bucket("Review",false,true,false)==5,"ignored precedence");
        verify(!CatalogQueueView.VisibleInTab("No changes available",false,false,false,1),"no-change not review");
        verify(CatalogQueueView.VisibleInTab("No changes available",false,false,false,3),"no-change tab");
        verify(CatalogQueueView.VisibleInTab("No changes available",false,false,false,6),"all tab");
        verify(!CatalogQueueView.VisibleInTab("Unmatched",false,false,false,1),"unmatched not manual review");
        verify(CatalogQueueView.ArchivedInReview("No changes available","NeedsReview"),"no-change archived in plan preview");
        verify(!CatalogQueueView.ArchivedInReview("Review","NeedsReview"),"real review not archived");
        verify(CatalogQueueView.ArchivedInReview("Review","Completed"),"completed plan archived");
        verify(CatalogQueueView.ReviewPriority(1|2|4|8,"description cover screenshot")==0,"actionable first");
        verify(CatalogQueueView.ReviewPriority(256,"description cover screenshot")==1,"series only later");
        verify(CatalogQueueView.ReviewPriority(1,"cover screenshot")==1,"nonoverlapping fields later");
        verify(CatalogQueueView.HighScoreTitleMismatch("70 Bump Bomb","Flame Beetles","Nintendo Game Boy Advance","1.000"),"unrelated title warning");
        verify(CatalogQueueView.HighScoreTitleMismatch("28 Prince of Persia","Prince of Persia","Nintendo Game Boy Advance","1.000"),"possible numbered prefix still warrants identity warning");
        verify(!CatalogQueueView.HighScoreTitleMismatch("Pokemon Leaf Green","Pokémon LeafGreen","Nintendo Game Boy Advance","1.000"),"accents normalized");
        verify(!CatalogQueueView.HighScoreTitleMismatch("70 Bump Bomb","Flame Beetles","Nintendo Game Boy Advance","0.65"),"low score not high-score warning");
        verify(CatalogQueueView.TitleMismatchWarning("28 Prince of Persia","Prince of Persia","Nintendo Game Boy Advance","0.660","Platform mismatch").Contains("platform"),"platform mismatch caution");
        verify(!CatalogQueueView.TitleMismatchWarning("70 Bump Bomb","Flame Beetles","Nintendo Game Boy Advance","1.000","exact title/alias evidence").Contains("100%"),"no false probability claim");
        int[] counts=new int[6];
        for(int i=0;i<376;i++)counts[CatalogQueueView.Bucket("No changes available",false,false,false)]++;
        for(int i=0;i<330;i++)counts[CatalogQueueView.Bucket("Completed",true,false,false)]++;
        for(int i=0;i<101;i++)counts[CatalogQueueView.Bucket("Review",false,false,false)]++;
        for(int i=0;i<28;i++)counts[CatalogQueueView.Bucket("Unmatched",false,false,false)]++;
        counts[CatalogQueueView.Bucket("Strong candidate",false,false,true)]++;
        counts[CatalogQueueView.Bucket("Ignored",false,true,false)]++;
        verify(counts[0]==1&&counts[1]==101&&counts[2]==28&&counts[3]==376&&counts[4]==330&&counts[5]==1,"837-row export bucket totals");
        Console.WriteLine("PASS: "+checks+" actionable review queue and alias warning checks. No Playnite data accessed.");
    }
}
