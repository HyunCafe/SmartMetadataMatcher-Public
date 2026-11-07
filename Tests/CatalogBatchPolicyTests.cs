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
        Assert(CatalogBatchPolicy.SupervisedLimit==5,"legacy supervised limit unchanged");
        Assert(CatalogBatchPolicy.ReviewedBulkLimit==1000,"explicitly reviewed resource ceiling");
        Assert(CatalogBatchPolicy.WithinLimit(46,false,true),"46 explicitly confirmed games accepted");
        Assert(CatalogBatchPolicy.WithinLimit(51,false,true),"51 explicitly reviewed games accepted");
        Assert(CatalogBatchPolicy.WithinLimit(837,false,true),"837 reviewed games accepted");
        var largeReviewed=Enumerable.Range(0,837).Select(i=>new CatalogBatchSelection{
            GameId=Guid.NewGuid(),IgdbId="10",CatalogTitle="Review",UserConfirmed=true}).ToList();
        Assert(CatalogBatchPolicy.ValidateSelection(largeReviewed,false,true)==null,
            "837 explicitly confirmed identities accepted at final gate");
        largeReviewed[836].UserConfirmed=false;
        Assert(CatalogBatchPolicy.ValidateSelection(largeReviewed,false,true)!=null,
            "last unconfirmed identity blocks entire large reviewed batch");
        Assert(!CatalogBatchPolicy.WithinLimit(1001,false,true),"1001 manually reviewed games rejected");
        Assert(CatalogBatchPolicy.IsStrictBatch(false,false,false),"fully strict reviews can batch");
        Assert(!CatalogBatchPolicy.IsStrictBatch(true,false,false),"blocked match not strict");
        Assert(!CatalogBatchPolicy.IsStrictBatch(false,true,false),"confirmed identity not strict");
        Assert(!CatalogBatchPolicy.IsStrictBatch(false,false,true),"manual exception not strict");
        Assert(CatalogBatchPolicy.WithinLimit(1,true),"single strict");
        Assert(CatalogBatchPolicy.WithinLimit(5,true),"five strict");
        Assert(CatalogBatchPolicy.WithinLimit(25,true),"25 strict");
        Assert(CatalogBatchPolicy.WithinLimit(26,true),"26 strict accepted");
        Assert(CatalogBatchPolicy.WithinLimit(837,true),"full library strict selection accepted");
        Assert(!CatalogBatchPolicy.WithinLimit(0,true),"empty batch blocked");
        Assert(CatalogBatchPolicy.WithinLimit(5,false),"five supervised");
        Assert(!CatalogBatchPolicy.WithinLimit(6,false),"six supervised blocked");
        Assert(!CatalogBatchPolicy.WithinLimit(-1,false),"negative size blocked");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(707,0,false)==707,"all strict-ready matches selectable");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(702,5,false)==702,"existing strict marks do not cap additional ready games");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(100,1,true)==100,"mixed selection can mark all, approval still required");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(100,5,true)==100,"mixed selection does not silently truncate");
        Assert(CatalogBatchPolicy.AvailableSelectionSlots(0,0,false)==0,"no ready matches");
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
        var many=Enumerable.Range(0,837).Select(i=>
            new CatalogBatchSelection {GameId=Guid.NewGuid(),IgdbId="3",CatalogTitle="A",AutoReady=true}).ToList();
        Assert(CatalogBatchPolicy.ValidateSelection(many,true)==null,
            "837 strict-ready games survive reviewed selection validation");
        many[836].FromSavedScan=true;
        Assert(CatalogBatchPolicy.ValidateSelection(many,true)!=null,
            "large strict selection rejects even one stale cached identity");
        many[836].FromSavedScan=false;
        many[836].GameId=many[0].GameId;
        Assert(CatalogBatchPolicy.ValidateSelection(many,true)!=null,
            "large strict selection rejects duplicates regardless of position");
        Assert(CatalogBatchPolicy.ValidateSelection(Enumerable.Range(0,6).Select(i=>
            new CatalogBatchSelection {GameId=Guid.NewGuid(),IgdbId="3",CatalogTitle="A",UserConfirmed=true}),false)!=null,
            "six manually confirmed games remain blocked");
        var reviewed=Enumerable.Range(0,46).Select(i=>new CatalogBatchSelection{
            GameId=Guid.NewGuid(),IgdbId="10",CatalogTitle="Reviewed game",
            UserConfirmed=true}).ToList();
        Assert(CatalogBatchPolicy.ValidateSelection(reviewed,false,true)==null,
            "46 manually confirmed games pass explicit reviewed selection");
        reviewed[20].UserConfirmed=false;
        Assert(CatalogBatchPolicy.ValidateSelection(reviewed,false,true)!=null,
            "single unchecked/unconfirmed identity blocks entire reviewed batch");
        reviewed[20].UserConfirmed=true;reviewed[20].FromSavedScan=true;
        Assert(CatalogBatchPolicy.ValidateSelection(reviewed,false,true)!=null,
            "cached identity blocks reviewed batch");
        reviewed[20].FromSavedScan=false;
        Assert(CatalogBatchPolicy.ValidateSelection(reviewed,true,true)!=null,
            "strict and manually reviewed batch modes cannot combine");
        Console.WriteLine("PASS: "+checks+" graduated batch-cap and fail-fast policy checks.");
    }
}
