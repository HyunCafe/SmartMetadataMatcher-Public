using System;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogBatchPolicyTests
{
    private static int checks;
    private static void Assert(bool pass,string why)
    {
        checks++;
        if(!pass)throw new Exception("FAIL: "+why);
    }

    internal static void Run()
    {
        Assert(CatalogBatchPolicy.StrictLimit==25,"strict limit unchanged");
        Assert(CatalogBatchPolicy.SupervisedLimit==5,"manual limit unchanged");
        Assert(CatalogBatchPolicy.IsStrictBatch(false,false,false),"fully strict reviews can batch");
        Assert(!CatalogBatchPolicy.IsStrictBatch(true,false,false),"blocked match not strict");
        Assert(!CatalogBatchPolicy.IsStrictBatch(false,true,false),"confirmed identity not strict");
        Assert(!CatalogBatchPolicy.IsStrictBatch(false,false,true),"manual exception not strict");
        Assert(CatalogBatchPolicy.WithinLimit(1,true),"single strict");
        Assert(CatalogBatchPolicy.WithinLimit(5,true),"five strict");
        Assert(CatalogBatchPolicy.WithinLimit(25,true),"25 strict");
        Assert(!CatalogBatchPolicy.WithinLimit(26,true),"26 strict blocked");
        Assert(!CatalogBatchPolicy.WithinLimit(0,true),"empty batch blocked");
        Assert(CatalogBatchPolicy.WithinLimit(5,false),"five supervised");
        Assert(!CatalogBatchPolicy.WithinLimit(6,false),"six supervised blocked");
        Assert(!CatalogBatchPolicy.WithinLimit(-1,false),"negative size blocked");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(0,false)==25,"first ready selection is capped at 25");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(5,false)==20,"existing marks consume slots");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(1,true)==4,"confirmed selections keep small cap");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(30,false)==0,"overfull selection cannot add");
        Assert(CatalogBatchPolicy.StopOnFailure(true),"any failure stops before next game");
        Assert(!CatalogBatchPolicy.StopOnFailure(false),"no error allows next game");
        var g1=Guid.NewGuid();var g2=Guid.NewGuid();
        var a=new CatalogBatchSelection {GameId=g1,IgdbId="6505",CatalogTitle="Mario vs. Donkey Kong",AutoReady=true};
        var b=new CatalogBatchSelection {GameId=g2,IgdbId="6505",CatalogTitle="Mario vs. Donkey Kong",AutoReady=true};
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{a,b},true)==null,
            "distinct Playnite copies may share a verified IGDB ID");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{a,a},true)!=null,
            "the same Playnite game must not be written twice in one batch");
        Assert(CatalogBatchPolicy.ValidateSelection(null,true)!=null,"missing batch rejected");
        Assert(CatalogBatchPolicy.ValidateSelection(new CatalogBatchSelection[0],true)!=null,"empty batch rejected");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="-1",CatalogTitle="A",AutoReady=true}},true)!=null,"bad IGDB ID");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="",AutoReady=true}},true)!=null,"empty catalog title");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=Guid.Empty,IgdbId="3",CatalogTitle="A",AutoReady=true}},true)!=null,"missing game id");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A",AutoReady=true,Ignored=true}},false)!=null,"ignored game blocked");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A",AutoReady=true,Completed=true}},false)!=null,"completed game blocked");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A"}},true)!=null,"unverified strict blocked");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A",AutoReady=true,FromSavedScan=true}},true)!=null,"cached strict blocked");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A",AutoReady=true,FromHistory=true}},true)!=null,"history-only strict blocked");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A",AutoReady=true,UserConfirmed=true}},true)!=null,"user-confirmed strict blocked");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A",UserConfirmed=true}},false)==null,"user-confirmed supervised allowed");
        Assert(CatalogBatchPolicy.ValidateSelection(new[]{new CatalogBatchSelection{GameId=g1,IgdbId="3",CatalogTitle="A"}},false)==null,"reviewed manual one-game match allowed");
        Assert(CatalogBatchPolicy.ValidateSelection(Enumerable.Range(0,26).Select(i=>
            new CatalogBatchSelection {GameId=Guid.NewGuid(),IgdbId="3",CatalogTitle="A",AutoReady=true}),true)!=null,
            "strict size checked before writes");
        Console.WriteLine("PASS: "+checks+" graduated batch-cap and fail-fast policy checks.");
    }
}
