using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogManualReviewFlowTests
{
    private static int checks;
    private static void Need(bool yes,string why)
    {checks++;if(!yes)throw new Exception("Manual review flow: "+why);}
    private static CatalogManualReviewCandidate Candidate()
    {return new CatalogManualReviewCandidate{GameId=Guid.NewGuid(),GameName="28 Prince of Persia",
        Platform="Nintendo Game Boy Advance",CatalogTitle="Prince of Persia",CatalogId="2438",
        PlannerCode="NeedsReview"};}
    internal static void Run()
    {
        var one=Candidate();
        Need(CatalogManualReviewFlow.Validate(new[]{one})==null,"one review candidate");
        var many=Enumerable.Range(0,46).Select(i=>Candidate()).ToList();
        Need(CatalogManualReviewFlow.Validate(many)==null,"46 reviewed candidates accepted");
        Need(CatalogManualReviewFlow.Validate(many.Take(0))!=null,"empty blocked");
        Need(CatalogManualReviewFlow.Validate(many.Concat(many.Take(5)))!=null,"51 blocked");
        Need(CatalogManualReviewFlow.Validate(new[]{many[0],many[0]})!=null,"duplicate game blocked");
        one.FromSavedScan=true;Need(CatalogManualReviewFlow.Validate(new[]{one})!=null,"stale blocked");
        one.FromSavedScan=false;one.PlannerCode="ConflictingCatalogId";
        Need(CatalogManualReviewFlow.Validate(new[]{one})!=null,"conflicting ID blocked");
        one.PlannerCode="NeedsReview";one.PriorFailure=true;
        Need(CatalogManualReviewFlow.Validate(new[]{one})!=null,"prior failure blocked");
        one.PriorFailure=false;one.Archived=true;
        Need(CatalogManualReviewFlow.Validate(new[]{one})!=null,"completed blocked");
        one.Archived=false;one.CatalogId="invalid";
        Need(CatalogManualReviewFlow.Validate(new[]{one})!=null,"invalid catalog ID blocked");
        one.CatalogId="2438";one.Platform="";
        Need(CatalogManualReviewFlow.Validate(new[]{one})!=null,"unknown platform blocked");
        Console.WriteLine("PASS: "+checks+" bounded explicit manual identity review checks.");
    }
}
