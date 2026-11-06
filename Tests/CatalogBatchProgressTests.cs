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
        Console.WriteLine("PASS: "+checks+" batch progress/cancellation presentation checks.");
    }
}
