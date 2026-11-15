using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class WorkspaceBatchReviewTests
{
    private static int checks;
    private static void Expect(bool pass,string title)
    {
        checks++;
        if (!pass)throw new Exception("FAIL Workspace batch preview: "+title);
    }
    private static WorkspaceBatchReviewInput Game(Guid id,string name,string candidate,bool confirmed)
    {
        return new WorkspaceBatchReviewInput {
            GameId=id, GameName=name, Platform="Nintendo Game Boy Advance",
            CandidateId="1942", CandidateTitle=candidate,MatchStatus="Strong candidate",
            MissingMask=(int)CatalogMissingField.Series,
            MissingCount=1, Marked=true,Saved=true,Confirmed=confirmed
        };
    }
    internal static void Run()
    {
        var one=Guid.NewGuid();var two=Guid.NewGuid();
        var games=new List<WorkspaceBatchReviewInput> {
            Game(one,"01 Golden Sun","Golden Sun",true),
            Game(two,"Other Game","Other Game",false)
        };
        games[1].CandidateId="1943";
        var library=games.Select(x=>Tuple.Create(x.GameId,x.GameName,x.Platform)).ToList();
        var plan=WorkspaceBatchReview.Preview(games,library);
        Expect(plan.Count==2,"marked scope retained");
        Expect(plan.Single(x=>x.GameId==one).ProposedTitle=="Golden Sun", "clean title is normal default");
        Expect(plan.Single(x=>x.GameId==one).CurrentTitle=="01 Golden Sun" &&
            plan.Single(x=>x.GameId==one).TargetTitle=="Golden Sun",
            "actual Playnite current and target titles are shown separately");
        Expect(plan.Single(x=>x.GameId==one).ReadyForPreflight, "manual confirmation only authorizes preflight");
        Expect(!plan.Single(x=>x.GameId==two).ReadyForPreflight, "saved result is not strict evidence");
        Expect(plan.Single(x=>x.GameId==two).NextStep.IndexOf("refresh",StringComparison.OrdinalIgnoreCase)>=0,
            "saved match requires refresh");
        games[1].Marked=false;
        Expect(WorkspaceBatchReview.Preview(games,library).Count==1,"unmarked excluded");
        games[1].Marked=true;
        games[1].GameId=one;
        bool rejected=false;
        try {WorkspaceBatchReview.Preview(games,library);}catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"duplicate Playnite game IDs fail closed");
        games[1].GameId=two;
        var custom=new CatalogImportDraft {GameId=one,IgdbId=1942,
            ExpectedName="01 Golden Sun",ExpectedPlatform="Nintendo Game Boy Advance",
            ExpectedCatalogTitle="Golden Sun",CustomTitleEnabled=true,CustomTitle="Other Game"};
        games[0].DraftState=new WorkspaceDraftState {Draft=custom,
            ExpectedMissingMask=games[0].MissingMask,ExpectedReleaseYear=0};
        plan=WorkspaceBatchReview.Preview(games,library);
        Expect(!plan.Single(x=>x.GameId==one).ReadyForPreflight,"custom title collision blocks readiness");
        Expect(plan.Single(x=>x.GameId==one).NextStep=="Resolve title collision","collision explains next step");
        custom.CustomTitle="Golden Sun Advance";
        plan=WorkspaceBatchReview.Preview(games,library);
        Expect(plan.Single(x=>x.GameId==one).ProposedTitle=="Golden Sun Advance", "validated manual title shown");
        games[0].DraftState.ExpectedReleaseYear=2005;
        plan=WorkspaceBatchReview.Preview(games,library);
        Expect(!plan.Single(x=>x.GameId==one).ReadyForPreflight,"stale draft cannot authorize preflight");
        Expect(plan.Single(x=>x.GameId==one).Draft.Contains("Stale"),"stale draft is conspicuous");
        // A title that is unique against the current library can still collide
        // with another proposed title inside the same selected batch.
        games[0].DraftState=null;
        var repeated=new List<WorkspaceBatchReviewInput> {
            Game(Guid.NewGuid(), "01 Golden Sun", "Golden Sun", true),
            Game(Guid.NewGuid(), "02 Golden Sun", "Golden Sun", true)
        };
        var duplicateLibrary=repeated.Select(x=>Tuple.Create(x.GameId,x.GameName,x.Platform)).ToList();
        var reserved=WorkspaceBatchReview.Preview(repeated,duplicateLibrary);
        Expect(reserved.Count==2,"batch reservation keeps both selected records");
        Expect(reserved[0].ProposedTitle=="Golden Sun", "first selected title receives canonical name");
        Expect(reserved[1].ProposedTitle=="Golden Sun (GBA)", "second proposed title is reserved with a safe platform suffix");
        Expect(reserved[0].ProposedTitle!=reserved[1].ProposedTitle, "same-batch duplicate display titles blocked");
        games[0].Ignored=true;
        rejected=false;
        try{WorkspaceBatchReview.Preview(games,library);}catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"ignored marked game blocked");
        var snapshot = new CatalogInput {Id=one,Name="01 Golden Sun",
            Platform="Nintendo Game Boy Advance",MissingMask=(int)CatalogMissingField.Series,
            MissingCount=1,LocalReleaseYear=0};
        var target = WorkspaceBatchLivePreflight.Capture(snapshot,1942,"Golden Sun",null);
        WorkspaceBatchLivePreflight.EnsureCurrent(target,snapshot,1942,"Golden Sun");
        Expect(target.GameId==one && target.IgdbId==1942,"exact field target binds game and catalog ID");
        rejected=false;
        try {WorkspaceBatchLivePreflight.EnsureCurrent(target,snapshot,1943,"Golden Sun");}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"changed exact IGDB ID rejected");
        snapshot.Name="Modified Playnite title";
        rejected=false;
        try {WorkspaceBatchLivePreflight.EnsureCurrent(target,snapshot,1942,"Golden Sun");}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"Playnite title changed during network request rejected");
        snapshot.Name="01 Golden Sun";
        snapshot.MissingMask=(int)CatalogMissingField.Description;
        snapshot.MissingCount=1;
        rejected=false;
        try {WorkspaceBatchLivePreflight.EnsureCurrent(target,snapshot,1942,"Golden Sun");}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"supported missing metadata changed during network request rejected");
        rejected=false;
        try {WorkspaceBatchLivePreflight.Capture(snapshot,-1,"Golden Sun",null);}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"invalid IGDB identity cannot start preflight");
        // Transfer is not approval: a fresh successful exact check is required
        // for every marked row. Canceled, stale, blocked or mixed batches fail.
        var gateId = Guid.NewGuid();
        var gateInput = new CatalogInput {Id=gateId,Name="01 Test",Platform="GBA",
            MissingMask=(int)CatalogMissingField.Cover,MissingCount=1,LocalReleaseYear=0};
        var gateTarget = WorkspaceBatchLivePreflight.Capture(gateInput,991,"Test",null);
        var gateRow = new WorkspaceBatchReviewRow {GameId=gateId,Game="01 Test",
            Platform="GBA",ProposedTitle="Test",
            LiveCheck="Not checked",NextStep="Needs confirmation"};
        var gateRows = new List<WorkspaceBatchReviewRow>{gateRow};
        var gateVerified = new Dictionary<Guid,WorkspaceLiveBatchTarget>();
        Expect(!WorkspaceBatchLivePreflight.ReadyForHandoff(gateRows,gateVerified),
            "unverified selected game cannot leave native review");
        gateVerified.Add(gateId,gateTarget);
        Expect(!WorkspaceBatchLivePreflight.ReadyForHandoff(gateRows,gateVerified),
            "valid snapshot without complete field check is insufficient");
        gateRow.LiveCheck="Fields checked · not approved";
        Expect(WorkspaceBatchLivePreflight.ReadyForHandoff(gateRows,gateVerified),
            "all checked rows can transfer for separate Classic approval");
        gateRow.LiveCheck="Blocked: IGDB mismatch";
        Expect(!WorkspaceBatchLivePreflight.ReadyForHandoff(gateRows,gateVerified),
            "failed field check blocks transfer");
        gateRow.LiveCheck="Fields checked · not approved";
        gateRow.NextStep="Resolve title collision";
        Expect(!WorkspaceBatchLivePreflight.ReadyForHandoff(gateRows,gateVerified),
            "unsafe proposed title blocks transfer");
        gateRow.NextStep="Needs confirmation";
        gateRows.Add(new WorkspaceBatchReviewRow {GameId=Guid.NewGuid(),ProposedTitle="Other",
            LiveCheck="Fields checked · not approved"});
        Expect(!WorkspaceBatchLivePreflight.ReadyForHandoff(gateRows,gateVerified),
            "partially checked multi-selection blocks transfer");
        gateRows.RemoveAt(1);
        var draftBefore = new CatalogImportDraft {GameId=gateId,IgdbId=991,
            ExpectedName="01 Test",ExpectedPlatform="GBA",ExpectedCatalogTitle="Test",
            CustomTitleEnabled=true,CustomTitle="Test"};
        var draftAfter = draftBefore.Copy();
        Expect(WorkspaceBatchLivePreflight.SameDraft(draftBefore,draftAfter),
            "identical draft preserved during field check");
        draftAfter.Genres=false;
        Expect(!WorkspaceBatchLivePreflight.SameDraft(draftBefore,draftAfter),
            "draft field opt-out changed after check blocks transfer");
        Expect(!WorkspaceBatchLivePreflight.SameDraft(draftBefore,null),
            "disappeared custom draft blocks transfer");
        Expect(WorkspaceBatchLivePreflight.SameDraft(null,null),
            "no-draft batch remains valid");
        gateRow.CurrentTitle="01 Test";
        gateRow.TargetTitle="Test";
        var titleDetail=new WorkspaceVerifiedFieldRow {GameId=gateId,Game="01 Test",
            Platform="GBA",IgdbId="991",Field="Display title",Current="01 Test",
            Igdb="Test",PlannedAction="Test"};
        var genreDetail=new WorkspaceVerifiedFieldRow {GameId=gateId,Game="01 Test",
            Platform="GBA",IgdbId="991",Field="Genres",Current="(missing)",
            Igdb="IGDB has genre",PlannedAction="Keep existing"};
        var detailMap=new Dictionary<Guid,List<WorkspaceVerifiedFieldRow>>();
        Expect(!WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "field-by-field plan required before transfer");
        detailMap[gateId]=new List<WorkspaceVerifiedFieldRow>{titleDetail,genreDetail};
        Expect(WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "exact checked per-field rows and target title form a reviewable plan");
        var copied=titleDetail.PlannedAction;
        titleDetail.PlannedAction="Wrong title";
        Expect(!WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "altered title plan rejects transfer");
        titleDetail.PlannedAction=copied;
        genreDetail.IgdbId="992";
        Expect(!WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "field plan from a different IGDB record rejects transfer");
        genreDetail.IgdbId="991";
        detailMap[gateId].Add(titleDetail);
        Expect(!WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "duplicate field row rejects transfer");
        detailMap[gateId].RemoveAt(2);
        gateRow.LiveCheck="Not checked";
        Expect(!WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "unverified status never promotes a stale field table");
        gateRow.LiveCheck="Fields checked · not approved";
        Expect(WorkspaceBatchLivePreflight.DetailedPlanReady(gateRows,gateVerified,detailMap),
            "valid exact field table remains reviewable without authorizing import");
        // A native review is independently checked when it crosses the Classic
        // window boundary. This is a transfer proof, not import authorization.
        var reviewContract=WorkspaceReviewContract.Capture(gateRows,gateVerified,detailMap);
        Expect(reviewContract.Count==1,"native review contract captures exact selected scope");
        // Native proposals must remain identical when Classic displays final approval.
        var expectedNativePlan=new List<Tuple<Guid,long,string,string,CatalogImportDraft>> {
            Tuple.Create(gateId,991L,"Test","Test",(CatalogImportDraft)null)
        };
        reviewContract.EnsureSamePlan(expectedNativePlan);
        Expect(reviewContract.DescribeVerifiedFields(gateId).Contains("Genres: Keep existing"),
            "Classic receives the exact verified native field summary");
        bool planBlocked=false;
        try {reviewContract.EnsureSamePlan(new List<Tuple<Guid,long,string,string,CatalogImportDraft>>());}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"dropping a reviewed game cannot authorize Classic approval");
        planBlocked=false;
        try {reviewContract.EnsureSamePlan(new [] {
            Tuple.Create(gateId,992L,"Test","Test",(CatalogImportDraft)null)});}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed IGDB ID cannot authorize Classic approval");
        planBlocked=false;
        try {reviewContract.EnsureSamePlan(new [] {
            Tuple.Create(gateId,991L,"Test","Unexpected rename",(CatalogImportDraft)null)});}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed final title cannot silently replace native plan");
        planBlocked=false;
        try {reviewContract.EnsureSamePlan(new [] {
            Tuple.Create(gateId,991L,"Test","Test",draftBefore)});}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"new staged draft cannot silently replace native field choices");
        planBlocked=false;
        try {reviewContract.DescribeVerifiedFields(Guid.NewGuid());}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"unreviewed game has no verified field summary");
        var unchanged=new CatalogInput {Id=gateId,Name="01 Test",Platform="GBA",
            MissingMask=(int)CatalogMissingField.Cover,MissingCount=1,LocalReleaseYear=0};
        reviewContract.EnsureCurrent(id=>id==gateId?unchanged:null);
        Expect(true,"Classic-side current snapshot accepted");
        // v0.5.44: revalidate *this* reviewed game on the UI thread immediately
        // before any Playnite update or journal mutation.
        reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Test", null);
        Expect(true,"unchanged verified game passes write-boundary gate");
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(Guid.NewGuid(), unchanged, 991L, "Test", "Test", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"unreviewed game cannot be written through native contract");
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 992L, "Test", "Test", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed IGDB ID blocked at write boundary");
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Wrong", "Test", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed IGDB title blocked at write boundary");
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Wrong", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed display title blocked at write boundary");
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Test", draftBefore);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed opt-in draft blocked at write boundary");
        unchanged.Platform="Unexpected platform";
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Test", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed Playnite platform blocked at write boundary");
        unchanged.Platform="GBA";
        unchanged.MissingMask=(int)CatalogMissingField.Description;
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Test", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed Playnite metadata gaps blocked at write boundary");
        unchanged.MissingMask=(int)CatalogMissingField.Cover;
        unchanged.LocalReleaseYear=2026;
        planBlocked=false;
        try {reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Test", null);}
        catch(InvalidOperationException){planBlocked=true;}
        Expect(planBlocked,"changed Playnite release year blocked at write boundary");
        unchanged.LocalReleaseYear=0;
        reviewContract.EnsureOneCurrent(gateId, unchanged, 991L, "Test", "Test", null);
        Expect(true,"unchanged next-game snapshot remains valid after other batch writes");
        unchanged.Name="Updated elsewhere";
        rejected=false;
        try {reviewContract.EnsureCurrent(id=>unchanged);}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"Classic-side stale title rejected");
        unchanged.Name="01 Test";
        unchanged.MissingMask=(int)CatalogMissingField.Series;
        rejected=false;
        try {reviewContract.EnsureCurrent(id=>unchanged);}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"Classic-side changed missing fields rejected");
        gateRow.Game="Different game";
        rejected=false;
        try {WorkspaceReviewContract.Capture(gateRows,gateVerified,detailMap);}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"forged native game name rejected");
        gateRow.Game="01 Test";
        gateRow.Platform="Unrelated platform";
        rejected=false;
        try {WorkspaceReviewContract.Capture(gateRows,gateVerified,detailMap);}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"forged native platform rejected");
        gateRow.Platform="GBA";
        gateRow.CurrentTitle="Unexpected title";
        titleDetail.Current="Unexpected title";
        rejected=false;
        try {WorkspaceReviewContract.Capture(gateRows,gateVerified,detailMap);}
        catch(InvalidOperationException){rejected=true;}
        Expect(rejected,"forged native current title rejected");
        Console.WriteLine("PASS: "+checks+" native batch preview / exact-field preflight / title safety checks.");
    }
}
