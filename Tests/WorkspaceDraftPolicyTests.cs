using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class WorkspaceDraftPolicyTests
{
    private static int count;
    private static void Check(bool ok, string message)
    {
        count++;
        if (!ok) throw new Exception("Workspace draft FAIL: " + message);
    }
    private static CatalogInput Input(int mask = 1 | 2 | 256)
    {
        return new CatalogInput { Id = Guid.NewGuid(), Name = "Example", Platform = "PC (Windows)",
            MissingMask = mask, MissingCount = CatalogEnrichmentNeeds.Count(mask), LocalReleaseYear = 2020 };
    }
    private static WorkspaceLiveComparisonResult Comparison(CatalogInput current)
    {
        return WorkspaceLiveComparison.Build(current, new CatalogGame {
            Id = 431, Name = "Example", Summary = "Detailed summary", CoverImageId = "coverABC123",
            SeriesNames = new List<string> {"Example Collection"}
        }, 431, "Example");
    }
    internal static void Run()
    {
        var input = Input();
        var compare = Comparison(input);
        var draft = WorkspaceDraftPolicy.New(input, compare);
        Check(draft.Description && draft.Cover && draft.Series, "new draft keeps eligible fields included");
        Check(WorkspaceDraftPolicy.MayInclude(compare, "Cover"), "cover available");
        Check(WorkspaceDraftPolicy.MayInclude(compare, "Series"), "series available");
        Check(!WorkspaceDraftPolicy.MayInclude(compare, "Background"), "missing remote field is unavailable");
        WorkspaceDraftPolicy.SetIncluded(draft, "Cover", false);
        WorkspaceDraftPolicy.SetIncluded(draft, "Series", false);
        Check(!WorkspaceDraftPolicy.Included(draft, "Cover") && !WorkspaceDraftPolicy.Included(draft, "Series"),
            "individual exclusions are independent");
        Check(WorkspaceDraftPolicy.IncludedAvailable(compare, draft) == 1, "included-available count follows exclusions");
        Check(WorkspaceDraftPolicy.ActionLabel(compare.Fields.First(x=>x.Field=="Cover"),draft)
            .StartsWith("Excluded from this draft", StringComparison.Ordinal), "preview shows exclusions");
        WorkspaceDraftPolicy.SetIncluded(draft, "Cover", true);
        Check(WorkspaceDraftPolicy.IncludedAvailable(compare, draft) == 2, "re-including restores count");
        var existing = WorkspaceLiveComparison.Build(Input(1), new CatalogGame {Id=431,Name="Example",
            Summary="summary"},431,"Example");
        Check(!WorkspaceDraftPolicy.MayInclude(existing,"Cover"),"existing cover cannot be selected for replacement");
        var library = new List<Tuple<Guid,string,string>> {Tuple.Create(Guid.NewGuid(), "Another game", "PC")};
        var state = WorkspaceDraftPolicy.Save(input,compare,draft,library);
        Check(state.Matches(input,431,"Example"),"draft binds exact game/title/platform/identity/mask");
        Check(state.Draft != draft, "stored draft is a defensive clone");
        draft.Series = true;
        Check(!state.Draft.Series,"dialog edits cannot mutate saved draft");
        Check(!state.Matches(input,432,"Example"),"changed IGDB ID invalidates draft");
        Check(!state.Matches(input,431,"Different"),"changed IGDB name invalidates draft");
        var changed = Input();changed.Id=input.Id;changed.Name="Renamed";
        Check(!state.Matches(changed,431,"Example"),"renamed Playnite game invalidates draft");
        changed.Name="Example";changed.MissingMask=1;changed.MissingCount=1;
        Check(!state.Matches(changed,431,"Example"),"changed missing fields invalidate draft");
        changed.MissingMask=input.MissingMask;changed.MissingCount=input.MissingCount;changed.LocalReleaseYear=2021;
        Check(!state.Matches(changed,431,"Example"),"release-year changes invalidate draft");
        var badComparison = Comparison(input);badComparison.IgdbId=432;
        bool blocked=false;
        try {WorkspaceDraftPolicy.Save(input,badComparison,draft,library);}catch(InvalidOperationException){blocked=true;}
        Check(blocked,"identity mismatch fails closed");
        var custom=WorkspaceDraftPolicy.New(input,compare);
        custom.CustomTitleEnabled=true;custom.CustomTitle="Another game";
        blocked=false;
        try {WorkspaceDraftPolicy.Save(input,compare,custom,library);}catch(InvalidOperationException){blocked=true;}
        Check(blocked,"colliding title cannot be staged");
        custom.CustomTitle="Example: Revisited";
        Check(WorkspaceDraftPolicy.Save(input,compare,custom,library)!=null,"safe custom title staged");
        custom.CustomDescriptionEnabled=true;custom.CustomDescription="";
        blocked=false;
        try {WorkspaceDraftPolicy.Save(input,compare,custom,library);}catch(InvalidOperationException){blocked=true;}
        Check(blocked,"empty custom description rejected");
        custom.CustomDescription="Reviewed description";
        Check(WorkspaceDraftPolicy.Save(input,compare,custom,library).Draft.CustomDescriptionEnabled,
            "valid custom description preserved");
        Check(WorkspaceDraftPolicy.IncludedAvailable(compare,null)==0,"absent draft is never import selection");
        Console.WriteLine("PASS: " + count + " in-memory Design C draft validation checks; no Playnite data accessed.");
    }
}
