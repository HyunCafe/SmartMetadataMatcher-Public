using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogSmartResolverTests
{
    private static int checks;
    private static void Expect(bool ok,string message)
    {
        checks++;
        if(!ok)throw new Exception("Smart resolver regression: "+message);
    }
    private static CatalogInput I(string title,string platform="Game Boy Advance",int localYear=0)
    {return new CatalogInput{Name=title,Platform=platform,LocalReleaseYear=localYear};}
    private static CatalogGame G(long id,string name,long platform=24,int type=0,int year=2004)
    {
        return new CatalogGame{Id=id,Name=name,GameTypeId=type,FirstReleaseYear=year,
            PlatformIds=new List<long>{platform},Platforms=new List<string>{platform==24?"Game Boy Advance":"PC (Windows)"},
            ReleaseDates=new List<CatalogRelease>{new CatalogRelease{PlatformId=platform,Year=year}},
            Aliases=new List<string>(),Summary="A distinct game entry with context provided by the catalog."};
    }
    private static CatalogMatch Resolve(CatalogInput input,bool complete,params CatalogGame[] list)
    {return CatalogSmartResolver.Resolve(input,list,CatalogCore.Choose(input,list),complete);}
    internal static void Run()
    {
        // Numeral normalization must preserve *identity*, not just text similarity.
        Expect(CatalogSmartResolver.Tokens("Ys 1").SequenceEqual(
            CatalogSmartResolver.Tokens("Ys I")),"Ys 1 equals Ys I");
        Expect(CatalogSmartResolver.Tokens("Orchid Ledger Chapter One and Two").SequenceEqual(
            CatalogSmartResolver.Tokens("Orchid Ledger Chapter I & II")),
            "explicit ordinal label supports a spelled-out pair without a franchise list");
        Expect(CatalogSmartResolver.Tokens("Final Fantasy 4").SequenceEqual(
            CatalogSmartResolver.Tokens("Final Fantasy IV")),
            "Final Fantasy 4 equals Final Fantasy IV");
        Expect(CatalogSmartResolver.Tokens("Final Fantasy 15").SequenceEqual(
            CatalogSmartResolver.Tokens("Final Fantasy XV")),
            "Roman numerals beyond X resolve");
        Expect(CatalogSmartResolver.Tokens("Orchid Ledger Chapter Seven").SequenceEqual(
            CatalogSmartResolver.Tokens("Orchid Ledger Chapter VII")),
            "spelled sequel number equals Roman numeral after an explicit ordinal label");
        Expect(!CatalogSmartResolver.Tokens("Ys I").SequenceEqual(
            CatalogSmartResolver.Tokens("Ys II")),"Ys I never equals Ys II");
        Expect(!CatalogSmartResolver.Tokens("Mega Man X").SequenceEqual(
            CatalogSmartResolver.Tokens("Mega Man 10")),
            "Mega Man X is a separate series, not installment 10");
        Expect(!CatalogSmartResolver.Tokens("The Day I Died").SequenceEqual(
            CatalogSmartResolver.Tokens("The Day 1 Died")),
            "English pronoun I is not treated as an ordinal");
        Expect(!CatalogSmartResolver.Tokens("Project X Zone").SequenceEqual(
            CatalogSmartResolver.Tokens("Project 10 Zone")),
            "X within a name is not automatically an ordinal");
        Expect(!CatalogSmartResolver.Tokens("Ys I & II Chronicles+").SequenceEqual(
            CatalogSmartResolver.Tokens("Ys I & II Chronicles")),
            "a trailing plus may identify a distinct edition");
        Expect(!Resolve(I("Ys I & II Chronicles","PC (Windows)"),true,
            G(10015,"Ys I & II Chronicles+",6,0,2013)).SmartAutoEvidence,
            "a plus-suffixed edition is not a silent replacement for the base game");
        Expect(!Resolve(I("Ys One And Two","PlayStation Portable"),true,
            G(10016,"Ys I & II Chronicles: Premium Edition",38,0,2009)).SmartAutoEvidence,
            "abbreviated PSP titles cannot guess the premium edition");
        var ysPair=G(21020,"Ys I & II Chronicles",38,3,2009);
        var ysPremium=G(42692,"Ys I & II Chronicles: Premium Edition",38,3,2011);
        ysPremium.VersionParentId=21020;
        ysPremium.VersionTitle="Premium Edition";
        Expect(!Resolve(I("Ys One And Two","Sony PlayStation Portable"),true,
            ysPair,ysPremium).SmartAutoEvidence,
            "abbreviated bundle without catalog evidence remains for review");
        Expect(Resolve(I("Ys I & II Chronicles","Sony PlayStation Portable"),true,
            ysPair,ysPremium).SmartAutoEvidence,
            "an exact standard bundle remains eligible with a premium rival");
        Expect(!Resolve(I("Ys One And Two","Sony PlayStation Portable"),true,
            ysPremium).SmartAutoEvidence,
            "Premium-only results must not be mistaken for standard Chronicles");
        Expect(!Resolve(I("Ys One And Two","Sony PlayStation Portable"),false,
            ysPair,ysPremium).SmartAutoEvidence,
            "incomplete IGDB coverage never authorizes the Chronicles shortcut");
        Expect(!Resolve(I("Ys One And Two","Sony PlayStation Portable"),true,
            G(21021,"Ys I & II Chronicles",38,0,2009)).SmartAutoEvidence,
            "an untyped-as-bundle pair cannot use the bundle-specific shortcut");
        Expect(!Resolve(I("Ys One And Two","Sony PlayStation Portable"),true,
            G(21022,"Ys III & IV Chronicles",38,3,2009)).SmartAutoEvidence,
            "different pairs of numbered releases cannot be conflated");
        Expect(!Resolve(I("Ys One And Two","Sony PlayStation Portable"),true,
            G(21023,"Ys I & II Chronicles",6,3,2013)).SmartAutoEvidence,
            "correct title on wrong platform cannot authorize a PSP bundle");
        Expect(!Resolve(I("Legend Of Heroes 1","Sony PlayStation Portable"),true,
            G(405426,"Legend of Heroes",38,0,2006)).SmartAutoEvidence,
            "generic franchise plus informal first numeral is insufficient for PSP identity");
        var bladeOriginal=G(23115,"Blade Dancer: Lineage of Light",38,0,2006);
        var bladeLaterPort=G(269079,"Blade Dancer: Lineage of Light",38,11,2023);
        bladeLaterPort.ParentGameId=23115;
        bladeLaterPort.ReleaseDates=new List<CatalogRelease>(); // no dated PSP release for port
        var blade=I("Blade Dancer Lineage Of Light","Sony PlayStation Portable");
        var bladeResult=Resolve(blade,true,bladeOriginal,bladeLaterPort);
        Expect(bladeResult.SmartAutoEvidence && bladeResult.Game.Id==23115,
            "original PSP title outranks an undated same-name port referencing it");
        bladeLaterPort.ReleaseDates=new List<CatalogRelease>{
            new CatalogRelease{PlatformId=38,Year=2023}};
        Expect(!Resolve(blade,true,bladeOriginal,bladeLaterPort).SmartAutoEvidence,
            "independently dated same-platform port requires identity corroboration");
        bladeLaterPort.ReleaseDates=new List<CatalogRelease>();
        Expect(!Resolve(I("Blade Dancer Lineage Of Light","Sony PlayStation Portable",2023),
            true,bladeOriginal,bladeLaterPort).SmartAutoEvidence,
            "conflicting local release year cannot be ignored to choose older original");
        bladeLaterPort.ParentGameId=null;
        Expect(!Resolve(blade,true,bladeOriginal,bladeLaterPort).SmartAutoEvidence,
            "a port without direct proven parent relationship remains a rival");
        bladeLaterPort.ParentGameId=23115;
        Expect(!Resolve(blade,false,bladeOriginal,bladeLaterPort).SmartAutoEvidence,
            "incomplete port-rival search must not auto-approve Blade Dancer");
        var ysOne=I("Ys 1","PC (Windows)");
        Expect(Resolve(ysOne,true,G(10001,"Ys I",6,0,1987)).SmartAutoEvidence,
            "Ys 1 resolves a unique Ys I candidate on the correct platform");
        Expect(!Resolve(ysOne,true,G(10002,"Ys II",6,0,1988)).SmartAutoEvidence,
            "Ys II cannot satisfy a request for Ys 1");
        var crash=I("Crash Bandicoot 2");
        var ntranced=G(1198,"Crash Bandicoot 2: N-Tranced",24,0,2003);
        var collection=G(600,"Crash Bandicoot 2: N-Tranced + Crash Nitro Kart",24,3,2006);
        var result=Resolve(crash,true,collection,ntranced);
        Expect(result.Game!=null&&result.Game.Id==1198&&result.SmartAutoEvidence,
            "GBA standalone dominates directly related compilation");
        Expect(result.MatchStatus=="Strong candidate"&&CatalogCore.CanAutoPlan(result),
            "fresh, complete, unique ordinal-prefix selection becomes ready");
        Expect(!Resolve(crash,false,collection,ntranced).SmartAutoEvidence,
            "incomplete IGDB candidate coverage cannot automatically approve");
        Expect(!Resolve(crash,true,collection).SmartAutoEvidence,
            "standalone identity absent: compilation is not a safe guess");
        Expect(!CatalogSmartResolver.LiveIdentityCompatible(crash,G(1,"Crash Bandicoot 3: N-Tranced")),
            "a different sequel number cannot pass final preflight");
        var changed=G(1198,"Crash Bandicoot 2: N-Tranced",24,1,2003);
        Expect(!CatalogSmartResolver.LiveIdentityCompatible(crash,changed),
            "live recheck blocks a provider record converted to DLC");
        var monument=I("Monument Valley","PC (Windows)");
        var orig=G(150,"Monument Valley",6,0,2014);
        var sequel=G(36749,"Monument Valley II",6,0,2017);
        result=Resolve(monument,true,sequel,orig);
        Expect(result.SmartAutoEvidence&&result.Game.Id==150,
            "Monument Valley original replaces incorrectly prioritized sequel");
        Expect(!Resolve(I("Monument Valley II","PC (Windows)"),true,orig).SmartAutoEvidence,
            "original cannot silently stand in for numbered sequel");
        var mario=I("Super Mario Advance 4 - Super Mario Bros 3");
        var bundle=G(16748,"Super Mario Advance 4: Super Mario Bros. 3",24,3,2003);
        Expect(Resolve(mario,true,bundle).SmartAutoEvidence,
            "explicit full-title GBA playable bundle is permitted");
        var sengoku=I("Sengoku","PC (Windows)");
        var a=G(5433,"Sengoku",6,0,2011);
        var b=G(8001,"Sengoku",6,0,1991);
        result=Resolve(sengoku,true,a,b);
        Expect(!result.SmartAutoEvidence,"two same-name PC games without independent year stay review");
        result=Resolve(I("Sengoku","PC (Windows)",2011),true,a,b);
        Expect(result.SmartAutoEvidence&&result.Game.Id==5433,
            "locally existing release year can disambiguate distant-year homonyms");
        result=Resolve(I("Sengoku","PC (Windows)",2011),true,a,G(8002,"Sengoku",6,0,2012));
        Expect(!result.SmartAutoEvidence,"close-year homonyms cannot be disambiguated");
        var alias=G(246318,"Flame Beetles"); alias.Aliases.Add("Bump Bomb");
        Expect(!Resolve(I("Bump Bomb"),true,alias).SmartAutoEvidence,
            "unrelated official game matched through an alias stays review");
        var unknownExact=G(123,"Ys I",6,0,1987); unknownExact.GameTypeId=null;
        Expect(!Resolve(ysOne,true,unknownExact).SmartAutoEvidence,
            "a full-name IGDB record with missing game type needs review");
        Expect(Resolve(I("The Walking Dead: Episode 2","PC (Windows)"),true,
            G(126,"The Walking Dead: Episode 2",6,6,2012)).SmartAutoEvidence,
            "exactly named, playable episodic releases can qualify");
        Expect(!Resolve(I("The Walking Dead","PC (Windows)"),true,
            G(126,"The Walking Dead: Episode 2",6,6,2012)).SmartAutoEvidence,
            "a base title must not become a numbered episode");
        Expect(Resolve(I("Named Standalone Expansion","PC (Windows)"),true,
            G(128,"Named Standalone Expansion",6,4,2016)).SmartAutoEvidence,
            "standalone expansions with exact full title are valid playable records");
        var unknown=G(12,"Crash Bandicoot 2: N-Tranced");unknown.GameTypeId=null;
        Expect(!Resolve(crash,true,unknown).SmartAutoEvidence,
            "unknown game type cannot approve shortened title");
        var typo=Resolve(I("Monster Kingdom Jewel Summone","PlayStation Portable"),true,
            G(42890,"Monster Kingdom: Jewel Summoner",38,0,2006));
        Expect(typo.SmartAutoEvidence&&typo.Game.Id==42890,
            "unique same-platform near-identical full title typo recovered");
        var ys=Resolve(I("Ys The Oath Of Felghana","PlayStation Portable"),true,
            G(15455,"Ys: The Oath in Felghana",38,0,2005));
        Expect(ys.SmartAutoEvidence,"minor function-word variation can be recovered");
        Expect(!Resolve(I("Monster Kingdom Jewel Summone","PlayStation Portable"),false,
            G(42890,"Monster Kingdom: Jewel Summoner",38,0,2006)).SmartAutoEvidence,
            "a typo is not auto-approved when rival search is truncated");
        var beta=G(333,"Back 4 Blood",6);
        Expect(!Resolve(I("Back 4 Blood Beta","PC (Windows)"),true,beta).SmartAutoEvidence,
            "beta is not interchangeable with base game");
        Expect(!Resolve(I("Mario Kart"),true,G(2343,"Mario Kart: Super Circuit")).SmartAutoEvidence,
            "two-word generic Mario Kart cannot auto-supply unknown subtitle");
        Expect(!Resolve(I("The Falconeer","PC (Windows)"),true,
            G(121709,"The Falconeer: Revolution Remaster",6,9)).SmartAutoEvidence,
            "different remaster identity must stay review");
        // Additional real-library title patterns from the 100-row review export.
        Expect(!Resolve(I("Lunar - Genesis","Nintendo DS"),true,
            G(85266,"Lunar Genesis: Dragon Song",20,0,2005)).SmartAutoEvidence,
            "two-word prefix alone is not evidence of the full DS identity");
        Expect(!Resolve(I("Astro Boy"),true,
            G(6310,"Astro Boy: Omega Factor",24,0,2003)).SmartAutoEvidence,
            "two-word prefix alone cannot infer a GBA subtitle");
        Expect(Resolve(I("Need For Speed Porsche"),true,
            G(248143,"Need for Speed: Porsche Unleashed",24,0,2004)).SmartAutoEvidence,
            "longer abbreviated prefix preserves identifying Porsche token");
        Expect(!Resolve(I("Mario Kart"),true,
            G(2343,"Mario Kart: Super Circuit",24,0,2001)).SmartAutoEvidence,
            "generic two-word series never guesses a title");
        Expect(!Resolve(I("The king of the fighter EX2"),true,
            G(49169,"The King of Fighters EX2: Howling Blood",24,0,2003)).SmartAutoEvidence,
            "a heavily shortened title no longer inherits a built-in correction");
        Expect(!Resolve(I("Naruto Ninja Council 1"),true,
            G(24066,"Naruto: Ninja Council",24,0,2003)).SmartAutoEvidence,
            "a trailing numeral needs evidence before it can be discarded");
        Expect(!Resolve(I("Naruto Ninja Council 2"),true,
            G(24066,"Naruto: Ninja Council",24,0,2003)).SmartAutoEvidence,
            "a second installment is never silently converted to the first");
        Expect(Resolve(I("Final Fantasy Dissidia","Sony PlayStation Portable"),true,
            G(391,"Dissidia Final Fantasy",38,0,2008)).SmartAutoEvidence,
            "same distinctive words in different order can resolve");
        var namedEdition=G(14147,"Jotun: Valhalla Edition",6,0,2015);
        namedEdition.VersionParentId=123; namedEdition.VersionTitle="Valhalla Edition";
        Expect(Resolve(I("Jotun Valhalla Edition","PC (Windows)"),true,
            namedEdition).SmartAutoEvidence,
            "explicit full-name edition can resolve despite version-parent link");
        Expect(!Resolve(I("Jotun","PC (Windows)"),true,
            namedEdition).SmartAutoEvidence,
            "an unspecified base game never inherits an edition-specific variant");
        var expanded=G(255419,"Mail Mole + 'Xpress Deliveries",6,10,2021);
        expanded.ParentGameId=132540;
        Expect(Resolve(I("Mail Mole + 'Xpress Deliveries","PC (Windows)"),true,
            expanded).SmartAutoEvidence,
            "full-title named expanded game may be matched as a separate release");
        var wrongDlc=G(238444,"Mail Mole + 'Xpress Deliveries",6,1,2021);
        Expect(!Resolve(I("Mail Mole + 'Xpress Deliveries","PC (Windows)"),true,
            wrongDlc).SmartAutoEvidence,
            "DLC type cannot be silently promoted as playable release");
        var established=G(121295,"Grime",6,0,2021);
        established.RatingCount=900;
        established.Summary=new string('x',100);
        var obscure=G(260887,"Grime",6,0,2022);
        obscure.RatingCount=3;
        Expect(!Resolve(I("GRIME","PC (Windows)"),true,
            obscure,established).SmartAutoEvidence,
            "popularity and a summary cannot identify the locally owned release");
        established.RatingCount=18;
        Expect(!Resolve(I("GRIME","PC (Windows)"),true,
            obscure,established).SmartAutoEvidence,
            "provider popularity without overwhelming margin does not prove identity");
        var oldSengoku=G(5433,"Sengoku",6,0,1991);
        oldSengoku.RatingCount=900;oldSengoku.Summary=new string('x',100);
        var newSengoku=G(27946,"Sengoku",6,0,2011);
        newSengoku.RatingCount=2;
        Expect(!Resolve(I("Sengoku","PC (Windows)"),true,
            oldSengoku,newSengoku).SmartAutoEvidence,
            "unrelated far-apart same-name releases still require local evidence");
        var unfamiliar=I("Orchid Ledger 4","PC (Windows)");
        var fourth=G(91001,"Orchid Ledger IV",6,0,2005);
        var fifth=G(91002,"Orchid Ledger V",6,0,2006);
        Expect(Resolve(unfamiliar,true,fifth,fourth).SmartAutoEvidence &&
            Resolve(unfamiliar,true,fifth,fourth).Game.Id==fourth.Id,
            "an unfamiliar series resolves its unambiguous Roman numeral");
        Expect(!Resolve(I("Orchid Ledger 10","PC (Windows)"),true,
            G(91003,"Orchid Ledger X",6,0,2007)).SmartAutoEvidence,
            "an unlabelled X in an unfamiliar series is not an ordinal");
        var original=G(91004,"Cobalt Harbor",38,0,2005);
        var port=G(91005,"Cobalt Harbor",38,11,2023);
        port.ParentGameId=original.Id;
        port.ReleaseDates=new List<CatalogRelease>();
        Expect(Resolve(I("Cobalt Harbor","PlayStation Portable"),true,port,original).SmartAutoEvidence &&
            Resolve(I("Cobalt Harbor","PlayStation Portable"),true,port,original).Game.Id==original.Id,
            "the port-lineage rule works without a known title");
        port.ParentGameId=null;
        Expect(!Resolve(I("Cobalt Harbor","PlayStation Portable"),true,port,original).SmartAutoEvidence,
            "a same-name port without a confirmed parent remains a rival");
        var duplicateRich=G(1198,"Crash Bandicoot 2: N-Tranced",24,0,2003);
        var duplicateSparse=G(1198,"Crash Bandicoot 2: N-Tranced",24,0,2003);
        duplicateSparse.GameTypeId=null;duplicateSparse.ReleaseDates=null;
        Expect(CatalogSmartResolver.MergeCandidates(new[]{duplicateSparse,duplicateRich})[0].GameTypeId==0,
            "duplicate ID merges prefer a fully typed and platform-dated record");
        var q=CatalogSmartResolver.BuildPrefixQuery("Crash Bandicoot 2","Game Boy Advance");
        Expect(q.Contains("where platforms = 24")&&q.Contains("limit 101")&&q.Contains("game_type"),
            "bounded platform-specific expanded IGDB query");
        Expect(CatalogSmartResolver.BuildPrefixQuery("Halo\"; where id = 1;","PC (Windows)")=="",
            "APICalypse injection rejected");
        Expect(CatalogSmartResolver.BuildPrefixQuery("Mail Mole + 'Xpress Deliveries","PC (Windows)")
            .Contains("name ~"),"punctuation in legitimate titles remains queryable");
        Expect(CatalogCore.BuildExactNameQueryBody("Mail Mole + 'Xpress Deliveries")
            .Contains("where name ~"),"exact-title lookup permits safe plus sign");
        Expect(CatalogSmartResolver.BuildPrefixQuery("Crash; where id = 1","Game Boy Advance")=="",
            "semicolon query injection fails closed");
        Expect(CatalogSmartResolver.BuildPrefixQuery("Crash Bandicoot 2","Unknown Platform")=="",
            "unknown platform cannot perform misleading query");
        // 60-case replay: distinctive franchise/suffix, GBA ports and PSP titles.
        var spongebob=G(210724,"SpongeBob SquarePants: Lights, Camera, Pants!",24,11,2005);
        spongebob.ParentGameId=2768;
        Expect(Resolve(I("Lights, Camera, Pants!"),true,spongebob).SmartAutoEvidence,
            "unique GBA SpongeBob port with exact suffix can resolve");
        Expect(!Resolve(I("Lights, Camera, Pants!"),true,
            G(210725,"Lights, Camera, Pants!: Special Edition",24,0,2005)).SmartAutoEvidence,
            "a named special edition cannot replace an unspecified base release");
        var pooh=G(243185,"Disney's Winnie the Pooh's Rumbly Tumbly Adventure",24,11,2005);
        pooh.ParentGameId=4234;
        Expect(Resolve(I("Winnie the Pooh's Rumbly Tumbly Adventure"),true,pooh).SmartAutoEvidence,
            "distinctive publisher prefix omission resolves the GBA port");
        var bumble=G(115575,"Wallace & Gromit's Grand Adventures: Episode 1 - Fright of the Bumblebees",6,6,2009);
        bumble.ParentGameId=11307;
        Expect(Resolve(I("Wallace & Gromit: Fright of the Bumblebees","PC (Windows)"),true,bumble).SmartAutoEvidence,
            "unique named episode subtitle can resolve without wrapper");
        var wrongEpisode=G(115576,"Wallace & Gromit's Grand Adventures: Episode 2 - Fright of the Bumblebees",6,6,2009);
        Expect(!CatalogSmartResolver.LiveIdentityCompatible(
            I("Wallace & Gromit: Episode 1 - Fright of the Bumblebees","PC (Windows)"),wrongEpisode),
            "an explicit different episode number is never discarded");
        var untold1=G(22508,"Untold Legends: Brotherhood of the Blade",38,0,2005);
        var untold2=G(23006,"Untold Legends: The Warrior's Code",38,0,2006);
        Expect(!Resolve(I("Untold Legends 1 - Brotherhood Of The Blade","Sony PlayStation Portable"),true,
            untold1).SmartAutoEvidence,"unverified first installment annotation requires review");
        Expect(!Resolve(I("Untold Legends 2 - The Warriors Code","Sony PlayStation Portable"),true,
            untold2).SmartAutoEvidence,"unverified second installment annotation requires review");
        Expect(!Resolve(I("Untold Legends 2 - The Warriors Code","Sony PlayStation Portable"),true,
            untold1).SmartAutoEvidence,"different Untold Legends subtitles cannot cross-match");
        var ysPort=G(200897,"Ys VI: The Ark of Napishtim",38,11,2006);
        ysPort.ParentGameId=15454;
        Expect(Resolve(I("Ys The Ark Of Napishtim","Sony PlayStation Portable"),true,
            ysPort).SmartAutoEvidence,"unique Ys VI PSP title can omit ordinal when subtitle is intact");
        var wrongOrdinal=G(200898,"Ys IV: The Ark of Napishtim",38,11,2006);
        wrongOrdinal.ParentGameId=15455;
        Expect(!Resolve(I("Ys The Ark Of Napishtim","Sony PlayStation Portable"),true,
            ysPort,wrongOrdinal).SmartAutoEvidence,
            "omitted ordinal cannot resolve when two numbered Ys entries share the subtitle");
        Expect(!Resolve(I("Ys II The Ark Of Napishtim","Sony PlayStation Portable"),true,
            ysPort).SmartAutoEvidence,
            "an explicitly different Ys numeral cannot be overridden by a subtitle match");
        Expect(!Resolve(I("Ys The Red Cave","Sony PlayStation Portable"),true,
            G(200899,"Ys VI: The Red Cave",38,11,2006)).SmartAutoEvidence,
            "generic short subtitle cannot authorize an omitted series ordinal");
        var onlyDlc=G(399154,"Jotunnslayer: Hordes of Hel - Conan",6,1,2026);
        onlyDlc.ParentGameId=296290;
        Expect(!Resolve(I("Jotunnslayer: Hordes of Hel","PC (Windows)"),true,
            onlyDlc).SmartAutoEvidence,"DLC cannot become parent game simply due to title prefix");
        Expect(!Resolve(I("Sudoku"),true,G(49233,"Sudoku Fever",24,0,2006),
            G(195493,"Sudoku Micro",24,0,2006)).SmartAutoEvidence,
            "generic Sudoku without ROM code cannot choose between GBA editions");
        Expect(!Resolve(I("Bubble Blaster"),true,
            G(257703,"Bubble Blasters",6,0,2023)).SmartAutoEvidence,
            "a non-GBA homonym cannot be auto-matched");
        Console.WriteLine("PASS: "+checks+" smart rival-ranking / truncated / identity-preflight checks.");
    }
}
