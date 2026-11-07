using System;
using SmartMetadataMatcher;

internal static class CatalogBatchProgressTests
{
    private static int checks;
    private static void Assert(bool value,string message)
    {
        checks++;
        if(!value)throw new Exception("FAIL: "+message);
    }
    internal static void Run()
    {
        var start=CatalogBatchProgress.Describe(5,0,0,"Checking Golden Sun",false,false);
        Assert(start.Contains("0 / 5 checked"),"initial count");
        Assert(start.Contains("Checking Golden Sun"),"current game");
        var next=CatalogBatchProgress.Describe(5,2,1,"Next game",false,false);
        Assert(next.Contains("3 / 5 checked")&&next.Contains("2 applied")&&next.Contains("1 failed"),"partial progress");
        Assert(CatalogBatchProgress.Describe(5,2,1,"",true,false).Contains("Stopping"),"cancel is after-current");
        var blocked=CatalogBatchProgress.Describe(5,2,1,"",true,true);
        Assert(blocked.Contains("RECOVERY REQUIRED")&&!blocked.Contains("Stopping"),"recovery overrides cancellation");
        Assert(!CatalogBatchProgress.Describe(2,2,0,"",false,false).Contains("RECOVERY"),"normal completion");
        var skipped=CatalogBatchProgress.Describe(30,20,9,1,"No changes",false,false);
        Assert(skipped.Contains("30 / 30 checked") && skipped.Contains("20 applied") &&
            skipped.Contains("9 skipped") && skipped.Contains("1 failed"),"skip counter and checked total");
        Assert(CatalogBatchProgress.Describe(4,1,2,1,"",false,true).Contains("RECOVERY REQUIRED"),
            "recovery warning preserved with skips");
        bool invalidSkip=false;
        try{CatalogBatchProgress.Describe(2,1,2,0,"",false,false);}
        catch(ArgumentOutOfRangeException){invalidSkip=true;}
        Assert(invalidSkip,"skips counted toward total capacity");
        bool negativeSkip=false;
        try{CatalogBatchProgress.Describe(2,0,-1,0,"",false,false);}
        catch(ArgumentOutOfRangeException){negativeSkip=true;}
        Assert(negativeSkip,"negative skip rejected");
        foreach(var bad in new[]{new[]{0,0,0},new[]{3,-1,0},new[]{3,1,-1},new[]{3,3,1}})
        {
            bool rejected=false;
            try{CatalogBatchProgress.Describe(bad[0],bad[1],bad[2],"",false,false);}
            catch(ArgumentOutOfRangeException){rejected=true;}
            Assert(rejected,"invalid batch counter rejected");
        }
        var validating=CatalogBatchProgress.DescribeIdentityPreflight(0,50,"Checking Naruto");
        Assert(validating.Contains("0 / 50 verified") && validating.Contains("No Playnite changes yet"),
            "preflight must show a zero-of-50 identity phase without claiming writes");
        var verified=CatalogBatchProgress.DescribeIdentityPreflight(25,50,"Verified Astro Boy");
        Assert(verified.Contains("25 / 50 verified") && verified.Contains("Verified Astro Boy"),
            "preflight shows live progress and game identity");
        Assert(CatalogBatchProgress.DescribeIdentityPreflight(50,50,"").Contains("50 / 50 verified"),
            "full preflight complete without marking games applied");
        foreach(var invalid in new[]{new[]{0,0},new[]{-1,50},new[]{51,50}})
        {
            bool rejected=false;
            try{CatalogBatchProgress.DescribeIdentityPreflight(invalid[0],invalid[1],"a");}
            catch(ArgumentOutOfRangeException){rejected=true;}
            Assert(rejected,"invalid preflight counts rejected");
        }
        Assert(CatalogBatchProgress.GroupLabel(0,431,50).Contains("Group 1 / 9"),
            "431 reviewed identities are displayed in bounded progress groups");
        Assert(CatalogBatchProgress.GroupLabel(49,431,50).Contains("Group 1 / 9"),
            "group boundary at 49");
        Assert(CatalogBatchProgress.GroupLabel(50,431,50).Contains("Group 2 / 9"),
            "group boundary advances at 50");
        Assert(CatalogBatchProgress.GroupLabel(431,431,50).Contains("Group 9 / 9"),
            "complete progress group does not roll beyond last group");
        Assert(CatalogBatchProgress.GroupLabel(1,1,50).Contains("Group 1 / 1"),
            "single-game group");
        bool invalidGroup=false;
        try {CatalogBatchProgress.GroupLabel(1,1,0);}catch(ArgumentOutOfRangeException){invalidGroup=true;}
        Assert(invalidGroup,"invalid zero group size blocked");
        Console.WriteLine("PASS: "+checks+" batch progress/cancellation presentation checks.");
    }
}
