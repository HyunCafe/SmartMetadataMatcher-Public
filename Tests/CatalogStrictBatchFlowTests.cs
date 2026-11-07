using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogStrictBatchFlowTests
{
    private static int checks;
    private static void Expect(bool ok,string label)
    {
        checks++;
        if(!ok)throw new Exception("Verified-batch test failed: "+label);
    }
    private static EnrichmentCandidate Good(int n)
    {
        return new EnrichmentCandidate {GameId=Guid.NewGuid(),CurrentName="Game "+n,
            Platform="PC (Windows)",CandidateTitle="Game "+n,CatalogId=(1000+n).ToString(),
            Status="Strong candidate",StrictEvidence=true,Marked=false};
    }
    private static HashSet<Guid> Ready(List<EnrichmentCandidate> rows)
    {
        var games=rows.Select(x=>Tuple.Create(x.GameId,x.CurrentName,x.Platform)).ToList();
        var decisions=EnrichmentJobPlanner.Create(rows,games,false,false);
        return CatalogStrictBatchFlow.EligibleIds(rows,decisions);
    }
    internal static void Run()
    {
        var rows=Enumerable.Range(0,358).Select(Good).ToList();
        var expected=new HashSet<Guid>(rows.Select(x=>x.GameId));
        var ready=Ready(rows);
        Expect(ready.Count==358 && ready.SetEquals(expected),
            "358 fresh strict-ready matches enter preflight without checkbox selections");
        foreach(var row in rows)row.Marked=true;
        Expect(Ready(rows).SetEquals(expected),"checkbox marks do not change strict-ready selection");
        var stale=Good(359);stale.FromSavedScan=true;stale.StrictEvidence=false;
        var uncertain=Good(360);uncertain.Status="Review";
        var manuallyConfirmed=Good(361);manuallyConfirmed.UserConfirmed=true;
        var ignored=Good(362);ignored.Ignored=true;
        var completed=Good(363);completed.Completed=true;
        var history=Good(364);history.FromHistory=true;
        var failed=Good(365);failed.PriorFailure=true;
        var notStrict=Good(366);notStrict.StrictEvidence=false;
        var blocked=new[]{stale,uncertain,manuallyConfirmed,ignored,completed,history,failed,notStrict};
        Expect(Ready(blocked.ToList()).Count==0,"stale, review, user-confirmed, ignored, done, history and failed are excluded");
        var duplicates=new[]{Good(400),Good(401)};
        duplicates[1].CatalogId=duplicates[0].CatalogId;
        duplicates[1].CandidateTitle="Different edition";
        Expect(Ready(duplicates.ToList()).Count==0,"conflicting IGDB ID still fails closed");
        var mixed=rows.Concat(blocked).ToList();
        Expect(Ready(mixed).Count==358,"unverified matches never leak into strict-ready action");
        Expect(Ready(new List<EnrichmentCandidate>()).Count==0,"empty matches never start a write plan");
        Expect(CatalogStrictBatchFlow.EligibleIds(null,null).Count==0,"null input fails closed");
        Console.WriteLine("PASS: "+checks+" primary verified-batch routing assertions; no Playnite access.");
    }
}
