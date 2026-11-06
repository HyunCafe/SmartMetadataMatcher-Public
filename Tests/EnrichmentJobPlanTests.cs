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
        Assert(plan.Count(x=>x.ReadyForPreflight)==3,
            "equivalent same-ID same-platform Playnite duplicates remain eligible");
        Assert(plan.Single(x=>x.GameId==a).RenameReason.Contains("Golden Sun (GBA)"),
            "numbered copy receives platform suffix automatically");
        doubled[2].Ignored=true;
        plan=EnrichmentJobPlanner.Create(doubled,names,true,false);
        Assert(plan.Single(x=>x.GameId==a).ReadyForPreflight &&
            plan.Single(x=>x.GameId==c).Code=="Ignored",
            "ignored equivalent library entry does not block eligible copy");
        doubled[2].Ignored=false;
        doubled[2].CandidateTitle="Golden Sun Special Edition";
        plan=EnrichmentJobPlanner.Create(doubled,names,true,false);
        Assert(plan.Count(x=>x.Code=="ConflictingCatalogId")==2,
            "same IGDB ID with distinct catalog identities is not silently accepted");
        doubled[2].CandidateTitle="Golden Sun";
        doubled[2].Platform="Nintendo DS";
        plan=EnrichmentJobPlanner.Create(doubled,names,true,false);
        Assert(plan.Count(x=>x.Code=="ConflictingCatalogId")==2,
            "cross-platform same-ID collision stays reviewable without independent proof");
        doubled[2].Platform="Game Boy Advance";
        rows[0].StrictEvidence=false;
        plan=EnrichmentJobPlanner.Create(rows,names,false,false);
        Assert(plan.Single(x=>x.GameId==a).Code=="NeedsReview", "strong label alone cannot authorize preflight");
        rows[0].FromSavedScan=true;
        plan=EnrichmentJobPlanner.Create(rows,names,false,false);
        Assert(plan.Single(x=>x.GameId==a).Code=="CachedNeedsRefresh" &&
            !plan.Single(x=>x.GameId==a).ReadyForPreflight &&
            plan.Single(x=>x.GameId==a).Explanation.Contains("refresh"),
            "cached Strong result requires targeted live refresh before a strict batch");
        rows[0].FromSavedScan=false;
        rows[0].Status="Review";rows[0].UserConfirmed=true;
        plan=EnrichmentJobPlanner.Create(rows,names,false,false);
        Assert(plan.Single(x=>x.GameId==a).Code=="UserConfirmed" &&
            plan.Single(x=>x.GameId==a).ReadyForPreflight,
            "explicit user-confirmed ID may enter supervised preflight without strict score");
        var manualDuplicate=rows.Concat(new[]{Candidate(c,1172,"Golden Sun")}).ToList();
        plan=EnrichmentJobPlanner.Create(manualDuplicate,names,false,false);
        Assert(plan.Single(x=>x.GameId==a).ReadyForPreflight &&
            plan.Single(x=>x.GameId==c).ReadyForPreflight,
            "user-confirmed and strict equivalent copies retain separate supervised approval");
        rows[0].UserConfirmed=false;rows[0].Status="Strong candidate";
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
        // Display state must never turn a cached Strong match into import authorization.
        Assert(EnrichmentJobPlanner.DashboardStatus("Strong candidate",false,false,true,false,false)=="Refresh required",
            "cached Strong candidate explicitly requires refresh");
        Assert(EnrichmentJobPlanner.DashboardStatus("Strong candidate",false,false,false,true,false)=="Ready for review",
            "fresh Strict match is only review-ready, not automatically applied");
        Assert(EnrichmentJobPlanner.DashboardStatus("Strong candidate",false,false,false,false,false)=="Needs review",
            "a Strong text label alone never grants readiness");
        Assert(EnrichmentJobPlanner.DashboardStatus("Review",false,false,true,false,false)=="Needs review",
            "saved ambiguous matches remain review cases");
        Assert(EnrichmentJobPlanner.DashboardStatus("Review",false,false,true,false,true)=="Confirmed — review",
            "user confirmed still requires live check and approval");
        Assert(EnrichmentJobPlanner.DashboardStatus("Review",true,false,false,true,true)=="Completed",
            "completed state takes precedence over other labels");
        Assert(EnrichmentJobPlanner.DashboardStatus("Strong candidate",false,true,false,true,false)=="Ignored",
            "ignored state takes precedence over strict evidence");
        Assert(EnrichmentJobPlanner.DashboardStatus("Unmatched",false,false,false,false,false)=="Unmatched",
            "unmatched status is explicit");
        Console.WriteLine("PASS: "+checks+" pure job-plan and recovery-gate checks.");
    }
}
