using System;
using System.Collections.Generic;
using SmartMetadataMatcher;

internal static class CatalogImportDraftTests
{
    private static int checks;
    static void Assert(bool yes,string message){checks++;if(!yes)throw new Exception("FAIL: "+message);}
    internal static void Run()
    {
        var id=Guid.NewGuid();var other=Guid.NewGuid();
        var title="Mario vs. Donkey Kong";
        var all=new List<Tuple<Guid,string,string>>{
            Tuple.Create(id,"07 Mario Vs Donkey Kong","Nintendo Game Boy Advance"),
            Tuple.Create(other,title,"Nintendo Game Boy Advance")};
        var draft=new CatalogImportDraft{GameId=id,IgdbId=6505,ExpectedName="07 Mario Vs Donkey Kong",
            ExpectedPlatform="Nintendo Game Boy Advance",ExpectedCatalogTitle=title};
        Assert(draft.Matches(id,draft.ExpectedName,draft.ExpectedPlatform,6505,title),"matching identity");
        Assert(!draft.Matches(id,draft.ExpectedName,draft.ExpectedPlatform,111,title),"changed ID invalidated");
        Assert(!draft.Matches(id,draft.ExpectedName,draft.ExpectedPlatform,6505,"Other"),"changed edition invalidated");
        Assert(!draft.Matches(id,"New name",draft.ExpectedPlatform,6505,title),"changed title invalidated");
        Assert(CatalogImportDraft.Valid(draft),"default draft valid");
        Assert(!draft.Customized(),"default draft not sticky");
        Assert(CatalogImportDraft.ResolveTitle(draft.ExpectedName,title,id,draft.ExpectedPlatform,all,true,draft)==
            "Mario Vs Donkey Kong (GBA)","automatic suffix from existing name");
        draft.CustomTitleEnabled=true;draft.CustomTitle="My Choice (GBA)";
        Assert(CatalogImportDraft.ResolveTitle(draft.ExpectedName,title,id,draft.ExpectedPlatform,all,false,draft)==
            "My Choice (GBA)","manual title independent of auto toggle");
        draft.CustomTitle="Mario vs. Donkey Kong";
        Assert(CatalogImportDraft.ResolveTitle(draft.ExpectedName,title,id,draft.ExpectedPlatform,all,false,draft)==null,
            "manual title cannot collide");
        draft.CustomTitle="bad\nname";
        Assert(!CatalogImportDraft.Valid(draft),"multiline game title rejected");
        draft.CustomTitle="Valid unique title";draft.CustomDescriptionEnabled=true;
        draft.CustomDescription="Custom text <tag> & newline\nNext line";
        Assert(CatalogImportDraft.Valid(draft),"safe escaped custom prose");
        Assert(MetadataPlan.HtmlFromSummary(draft.CustomDescription).Contains("&lt;tag&gt;"),
            "HTML is encoded");
        Assert(!CatalogImportDraft.SafeDescription(" "),"empty custom prose rejected");
        Assert(!CatalogImportDraft.SafeDescription(new string('a',12001)),"oversized prose rejected");
        draft.Cover=false;
        Assert(draft.Customized(),"field exclusion counts as change");
        var copy=draft.Copy();copy.CustomTitle="Different";copy.Cover=true;
        Assert(draft.CustomTitle=="Valid unique title" && !draft.Cover,"frozen copy protects original");
        Assert(CatalogImportDraft.ResolveTitle(draft.ExpectedName,title,id,draft.ExpectedPlatform,all,true,null)==
            "Mario Vs Donkey Kong (GBA)","no draft keeps old title behavior");
        Console.WriteLine("PASS: "+checks+" in-app import draft, collision and HTML safety checks.");
    }
}
