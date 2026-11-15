using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class WorkspaceLiveComparisonTests
{
    private static int count;
    private static void Check(bool result, string label)
    {
        count++;
        if (!result) throw new Exception("Live preview FAIL: " + label);
    }
    private static CatalogInput Input(int mask, string platform="PC (Windows)")
    {
        return new CatalogInput {Id=Guid.NewGuid(),Name="Sample Game",Platform=platform,
            MissingMask=mask,MissingCount=CatalogEnrichmentNeeds.Count(mask)};
    }
    private static CatalogGame Catalog()
    {
        return new CatalogGame {Id=123,Name="Sample Game",Summary="A game summary",
            CoverImageId="abc123",ScreenshotImageId="def456",
            Genres=new List<string>{"Role-playing"},Developers=new List<string>{"Studio"},
            Publishers=new List<string>{"Publisher"},SeriesNames=new List<string>{"Collection"}};
    }
    internal static void Run()
    {
        var current=Input(1|2|4|8|32|64|128|256);
        var game=Catalog();
        var output=WorkspaceLiveComparison.Build(current,game,123,"Sample Game");
        Check(output.PossibleAdditions==8,"all eight available non-release gaps counted");
        Check(output.Fields.Any(x=>x.Field=="Series" && x.MayAdd),"series shown when available");
        Check(output.Fields.Any(x=>x.Field=="Display title" && !x.MayAdd),"rename never automatically proposed");
        Check(output.Fields.All(x=>x.Field!="Release date"),"existing release field omitted");
        var onlySeries=WorkspaceLiveComparison.Build(Input(256),game,123,"Sample Game");
        Check(onlySeries.PossibleAdditions==1,"one available Series addition");
        var missingSeries=Catalog();missingSeries.SeriesNames=null;
        Check(WorkspaceLiveComparison.Build(Input(256),missingSeries,123,"Sample Game").PossibleAdditions==0,
            "missing remote Series is not a change");
        var existingCover=WorkspaceLiveComparison.Build(Input(1),game,123,"Sample Game");
        Check(existingCover.Fields.Any(x=>x.Field=="Cover" && x.Current=="Present" && !x.MayAdd),
            "existing cover retained");
        var invalidCover=Catalog();invalidCover.CoverImageId="not/valid";
        Check(!WorkspaceLiveComparison.Build(Input(2),invalidCover,123,"Sample Game")
            .Fields.Single(x=>x.Field=="Cover").MayAdd,"unsafe cover ID not proposed");
        var port=Input(16,"Nintendo Game Boy Advance");
        var release=Catalog();
        release.FirstReleaseYear=2000;
        release.ReleaseDates=new List<CatalogRelease>{
            new CatalogRelease {PlatformId=99999,Year=2000}};
        Check(WorkspaceLiveComparison.Build(port,release,123,"Sample Game").PossibleAdditions==0,
            "generic/wrong-platform release year cannot fill port date");
        release.ReleaseDates.Add(new CatalogRelease {PlatformId=CatalogCore.PlatformId(port.Platform),Year=2003});
        Check(WorkspaceLiveComparison.Build(port,release,123,"Sample Game")
            .Fields.Any(x=>x.Field=="Release date" && x.MayAdd && x.PossibleAction.Contains("2003")),
            "only matching platform-specific release year shown");
        bool rejected=false;
        try {WorkspaceLiveComparison.Build(current,game,456,"Sample Game");}
        catch(InvalidOperationException) {rejected=true;}
        Check(rejected,"wrong exact ID rejected");
        rejected=false;
        try {WorkspaceLiveComparison.Build(current,game,123,"Different Edition");}
        catch(InvalidOperationException) {rejected=true;}
        Check(rejected,"changed exact title rejected");
        rejected=false;
        var malformed=Input(256);malformed.MissingCount=9;
        try {WorkspaceLiveComparison.Build(malformed,game,123,"Sample Game");}
        catch(InvalidOperationException) {rejected=true;}
        Check(rejected,"inconsistent missing field mask rejected");
        Console.WriteLine("PASS: " + count + " read-only exact-ID field comparison checks; no Playnite access.");
    }
}
