using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class EnrichmentJobPlanTests
{
    private static int checks;
    private static void Assert(bool condition,string label)
    {
        checks++;
        if(!condition) throw new Exception("Job plan test failed: "+label);
    }
    private static EnrichmentCandidate Candidate(Guid gameId,long id,string name)
    {
        return new EnrichmentCandidate {GameId=gameId,CatalogId=id.ToString(),
            CurrentName="01 "+name, CandidateTitle=name,Platform="Game Boy Advance",
            Status="Strong candidate",StrictEvidence=true};
    }
    internal static void Run()
    {
        var a=Guid.NewGuid(); var b=Guid.NewGuid(); var c=Guid.NewGuid();
        var names=new List<Tuple<Guid,string,string>> {
            Tuple.Create(a,"01 Golden Sun","Game Boy Advance"),
            Tuple.Create(b,"02 Sonic Advance","Game Boy Advance"),
            Tuple.Create(c,"Golden Sun","Game Boy Advance")
        };
        var rows=new List<EnrichmentCandidate> {Candidate(a,1172,"Golden Sun"),
            Candidate(b,6283,"Sonic Advance")};
        var plan=EnrichmentJobPlanner.Create(rows,names,true,false);
        Assert(plan.Count==2 && plan.All(x=>x.ReadyForPreflight),"two strict candidates ready for exact-ID preflight");
        Assert(plan.Single(x=>x.GameId==a).AllowRename &&
            plan.Single(x=>x.GameId==a).RenameReason.Contains("Golden Sun (GBA)"),
            "same-platform duplicate gets GBA suffix during approved import");
        Assert(plan.Single(x=>x.GameId==b).AllowRename,"noncolliding rename allowed");
        var doubled=rows.Concat(new[]{Candidate(c,1172,"Golden Sun")}).ToList();
        plan=EnrichmentJobPlanner.Create(doubled,names,true,false);
        Assert(plan.Count(x=>x.Code=="DuplicateCatalogId")==2,
            "duplicate IGDB ID across full scan blocks both identities");
        Assert(plan.Count(x=>x.ReadyForPreflight)==1,"unrelated strict ID remains eligible");
        doubled[2].Ignored=true;
        plan=EnrichmentJobPlanner.Create(doubled,names,true,false);
        Assert(plan.Single(x=>x.GameId==a).Code=="DuplicateCatalogId",
            "ignored record cannot hide an IGDB duplicate already known to the scan");
        rows[0].StrictEvidence=false;
        plan=EnrichmentJobPlanner.Create(rows,names,false,false);
        Assert(plan.Single(x=>x.GameId==a).Code=="NeedsReview", "strong label alone cannot authorize preflight");
        rows[0].StrictEvidence=true; rows[0].Marked=false;rows[1].Marked=true;
        plan=EnrichmentJobPlanner.Create(rows,names,true,true);
        Assert(plan.Single(x=>x.GameId==a).Code=="NotSelected" &&
            plan.Single(x=>x.GameId==b).ReadyForPreflight,"marked scope is independent of candidate eligibility");
        rows[1].Completed=true;
        plan=EnrichmentJobPlanner.Create(rows,names,false,false);
        Assert(plan.Single(x=>x.GameId==b).Code=="Completed","completed record excluded from fresh plan");
        rows[1].Completed=false;rows[1].PriorFailure=true;
        plan=EnrichmentJobPlanner.Create(rows,names,false,false);
        Assert(plan.Single(x=>x.GameId==b).Code=="PriorFailure","failed record requires review before retry");
        Assert(!EnrichmentPlan.HasUnresolvedJournalEntry(new EnrichmentEntry[0]),"empty journal is safe");
        Assert(EnrichmentPlan.HasUnresolvedJournalEntry(new[]{new EnrichmentEntry{Committed=false}}),
            "uncommitted journal globally blocks new writes");
        Assert(!EnrichmentPlan.HasUnresolvedJournalEntry(new[]{new EnrichmentEntry{Committed=true}}),
            "committed journal does not block next operation");
        Console.WriteLine("PASS: "+checks+" pure job-plan and recovery-gate checks.");
    }
}
