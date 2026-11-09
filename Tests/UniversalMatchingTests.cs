using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SmartMetadataMatcher;

// Metamorphic checks: changing the franchise, supported platform, catalog ID,
// popularity or candidate order must not change the identity rule being tested.
// These are synthetic records, not measured IGDB accuracy or live-library replay.
internal static class UniversalMatchingTests
{
    private static int checks;
    private static void Check(bool condition,string label)
    {
        checks++;
        if(!condition)throw new Exception("Universal matching: "+label);
    }
    private static CatalogGame Game(long id,string name,long platform,int type=0)
    {
        return new CatalogGame { Id=id,Name=name,PlatformIds=new List<long>{platform},
            GameTypeId=type,FirstReleaseYear=2004,
            ReleaseDates=new List<CatalogRelease>{new CatalogRelease{PlatformId=platform,Year=2004}},
            Aliases=new List<string>(),Summary="Synthetic catalog context for an independent identity fixture." };
    }
    private static CatalogInput Input(string name,string platform)
    { return new CatalogInput { Name=name,Platform=platform }; }
    private static CatalogMatch Match(CatalogInput input,bool complete,params CatalogGame[] games)
    { return CatalogSmartResolver.Resolve(input,games,CatalogCore.Choose(input,games),complete); }
    internal static void Run()
    {
        var platforms=new[]{Tuple.Create("PC (Windows)",6L),Tuple.Create("Nintendo DS",20L),
            Tuple.Create("Game Boy Color",22L),Tuple.Create("Game Boy Advance",24L),
            Tuple.Create("Game Boy",33L),Tuple.Create("Nintendo 3DS",37L),
            Tuple.Create("PlayStation Portable",38L),Tuple.Create("PlayStation",7L),
            Tuple.Create("PlayStation 2",8L),Tuple.Create("Nintendo Switch",130L)};
        var series=new[]{"Aster", "Copper Horizon", "Lanterns Beyond Tomorrow", "星空 航海", "Lumiere du Nord"};
        foreach(var platform in platforms)
        foreach(var name in series)
        {
            var title=name+" IV";
            var input=Input(name+" 4",platform.Item1);
            var right=Game(901,title,platform.Item2);
            var wrong=Game(902,name+" VI",platform.Item2);
            var result=Match(input,true,wrong,right);
            Check(result.SmartAutoEvidence && result.Game.Id==right.Id && CatalogCore.CanAutoPlan(result),
                "Roman ordinal across series/platform: "+name+" / "+platform.Item1);
            var reverse=Match(input,true,right,wrong);
            Check(reverse.SmartAutoEvidence && reverse.Game.Id==right.Id,"candidate order invariance");
            Check(!CatalogCore.CanAutoPlan(Match(input,true,wrong)),"different sequel blocked");
            right.Id=13001; wrong.Id=7; wrong.RatingCount=1000000;
            Check(Match(input,true,wrong,right).Game.Id==right.Id,"IDs and popularity are not identity");
            Check(!CatalogCore.CanAutoPlan(Match(input,false,right)),"incomplete Roman lookup does not promote");
            var homonym=Game(13002,title,platform.Item2);
            Check(!CatalogCore.CanAutoPlan(Match(input,true,right,homonym)),"same-title rival keeps review");
            Check(!CatalogCore.CanAutoPlan(Match(input,true,Game(20,title,999))),"wrong platform blocked");
            var literal=Input(name,platform.Item1);
            Check(CatalogCore.CanAutoPlan(Match(literal,true,Game(31,name,platform.Item2))),"literal title supported");
            foreach(var prefix in new[]{"03 ","24 "})
            {
                var dirty=Input(prefix+name,platform.Item1);
                Check(CatalogCore.QueryTitle(dirty.Name,dirty.Platform)==dirty.Name,"numeric title preserved");
                Check(CatalogCore.QueryVariants(dirty.Name,dirty.Platform).Any(x=>x.Text==name && !x.StrongEvidence),
                    "possible list index is discovery only on every platform");
                Check(!CatalogCore.CanAutoPlan(Match(dirty,true,Game(32,name,platform.Item2))),"index removal is not approval");
                Check(CatalogCore.CanAutoPlan(Match(dirty,true,Game(33,dirty.Name,platform.Item2))),
                    "real numeric title can match literally");
            }
            foreach(var qualifier in new[]{"Remastered","Deluxe Edition","Redux","HD","+","++"})
            {
                Check(!CatalogCore.CanAutoPlan(Match(literal,true,Game(40,name+" "+qualifier,platform.Item2))),
                    "edition cannot be silently added");
                Check(!CatalogCore.CanAutoPlan(Match(Input(name+" "+qualifier,platform.Item1),true,Game(41,name,platform.Item2))),
                    "edition cannot be silently dropped");
            }
            var unknown=Game(50,name,platform.Item2);unknown.GameTypeId=null;
            Check(!CatalogCore.CanAutoPlan(Match(literal,true,unknown)),"fast path needs known release type");
            Check(!CatalogCore.CanAutoPlan(Match(literal,true,Game(51,name,platform.Item2,1))),"exact DLC cannot bypass type gate");
            var subtitle="Lanterns Beneath Winter Skies";
            var unnumbered=Input(name+": "+subtitle,platform.Item1);
            Check(Match(unnumbered,true,Game(61,name+" IV: "+subtitle,platform.Item2)).SmartAutoEvidence,
                "omitted catalog ordinal after arbitrary series length");
            Check(!Match(unnumbered,true,Game(61,name+" IV: "+subtitle,platform.Item2),
                Game(62,name+" VI: "+subtitle,platform.Item2)).SmartAutoEvidence,"omitted ordinal rival blocks promotion");
            foreach(var ordinal in new[]{"1","2","7","23"})
                Check(!CatalogCore.CanAutoPlan(Match(Input(name+" "+ordinal+": "+subtitle,platform.Item1),true,
                    Game(63,name+": "+subtitle,platform.Item2))),"local installment cannot be invented or discarded");
            Check(Match(Input(name+": "+subtitle,platform.Item1),true,
                Game(65,name+": Chapter 4 - "+subtitle,platform.Item2,6)).SmartAutoEvidence,
                "explicit episode wrapper after any series length");
        }
        foreach(var name in new[]{"Aster","Copper Horizon","Lanterns Beyond Tomorrow"})
        {
            var local=Input(name+" 4: Northern Lights","PC (Windows)");
            Check(!CatalogCore.CanAutoPlan(Match(local,true,Game(90,name+" 4: Northern Lights 2",6))),
                "prefix does not silently add a second sequel number");
            var alias=Game(91,name+" VI",6);alias.Aliases.Add(name+" IV");
            Check(!CatalogCore.CanAutoPlan(Match(Input(name+" IV","PC (Windows)"),true,alias)),
                "alias does not override conflicting canonical ordinal");
            Check(!CatalogCore.CanAutoPlan(Match(Input(name+"+","PC (Windows)"),true,Game(92,name+"++",6))),
                "plus count identifies a distinct version");
        }
        Check(CatalogCore.PlatformId("Sony PlayStation 2")==8,"vendor-prefixed supported alias");
        Check(CatalogCore.PlatformId("Nintendo DS, PC (Windows)")==0,"multi-platform string is not one platform");
        Check(CatalogCore.PlatformId("Unofficial Game Boy Advance Clone")==0,"substring is not a verified platform");
        Check(!CatalogCore.CanAutoPlan(Match(Input("Copper Horizon","Unmapped Console"),true,
            Game(100,"Copper Horizon",6))),"unmapped platform never guessed");
        Check(CatalogCore.QueryVariants("03 Copper Horizon","Unmapped Console")
            .Any(x=>x.Text=="Copper Horizon"&&!x.StrongEvidence),"discovery still works on unmapped platforms");
        foreach(var platform in new[]{"PC","GBA","PSP","PS2","Switch"})
        {
            var full="Copper Horizon "+platform;
            Check(CatalogCore.DiscoveryTitle(full,platform)=="Copper Horizon","platform suffix discovery is symmetric");
            Check(!CatalogCore.CanAutoPlan(Match(Input(full,platform),true,
                Game(101,"Copper Horizon",CatalogCore.PlatformId(platform)))),"platform tag removal cannot approve");
        }
        Check(CatalogCore.BuildSingleQueryBody("N++").Contains("N++"),"search preserves plus edition count");
        Check(CatalogCore.BuildSingleQueryBody("N").Length>0 && CatalogCore.BuildExactNameQueryBody("N").Length>0,
            "short legitimate titles are discoverable");
        Check(EnrichmentPlan.IsSafeTitle("N"),"short catalog title may be displayed");
        Check(EnrichmentPlan.SuggestedDisplayTitle("03 Copper Horizon","Copper Horizon+")=="Copper Horizon+",
            "display cleanup never erases catalog plus version");
        var batch=new[]{new GameDescriptor{Id=Guid.NewGuid(),Name="01 Copper Horizon",Platform="GBA",Source="Emulator"},
            new GameDescriptor{Id=Guid.NewGuid(),Name="02 Aster",Platform="GBA",Source="Emulator"},
            new GameDescriptor{Id=Guid.NewGuid(),Name="03 Lanterns",Platform="GBA",Source="Emulator"},
            new GameDescriptor{Id=Guid.NewGuid(),Name="24 Hours",Platform="GBA",Source="Emulator"}};
        Check(NameRules.Analyze(batch).All(x=>x.Status=="Review"),"sequence cannot prove a real numeric title is an index");
        var before=CultureInfo.CurrentCulture;
        try
        {
            foreach(var culture in new[]{"en-US","tr-TR","ja-JP"})
            {
                CultureInfo.CurrentCulture=new CultureInfo(culture);
                Check(Match(Input("Copper Horizon 4","PC (Windows)"),true,
                    Game(110,"Copper Horizon IV",6)).SmartAutoEvidence,"locale-independent numeral rules");
            }
        }
        finally {CultureInfo.CurrentCulture=before;}
        Console.WriteLine("PASS: "+checks+" universal matching invariance and counterexample checks.");
    }
}
