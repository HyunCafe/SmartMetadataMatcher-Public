using System;
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
        Console.WriteLine("PASS: "+checks+" graduated batch-cap and fail-fast policy checks.");
    }
}
