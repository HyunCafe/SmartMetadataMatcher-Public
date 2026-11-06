using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogCoreTests
{
    private static int checks;
    private static void Assert(bool good, string why)
    {
        checks++;
        if(!good) throw new Exception("Catalog test: "+why);
    }
    internal static void Run()
    {
        Assert(CatalogCore.IsTestingOnly("Chivalry 2 - Public Testing"), "Public Testing excluded");
        Assert(CatalogCore.IsTestingOnly("Paladins - Public Test"), "Public Test excluded");
        Assert(CatalogCore.IsTestingOnly("Insurgency: Sandstorm Community Test Environment"), "community test client excluded");
        Assert(CatalogCore.IsTestingOnly("CHIVALRY 2 - PUBLIC TESTING"), "case-insensitive exclusion");
        Assert(!CatalogCore.IsTestingOnly("Test Drive Unlimited"), "normal Test title retained");
        Assert(!CatalogCore.IsTestingOnly("The Public Test of Time"), "title containing test words retained");
        Assert(!CatalogCore.IsTestingOnly("Mazes of Fates DS Beta"), "unrelated beta not silently excluded");
        Assert(CatalogCore.NextScanCount(120,0)==20, "20-game selection");
        Assert(CatalogCore.NextScanCount(120,1)==50, "50-game selection");
        Assert(CatalogCore.NextScanCount(120,2)==120, "all remaining selection");
        Assert(CatalogCore.NextScanCount(12,1)==12, "remaining count is bounded");
        Assert(CatalogCore.NextScanCount(0,2)==0, "empty queue yields no scan");
        Assert(CatalogCore.QueryTitle("01 Pokemon Fire Red", "Nintendo Game Boy Advance") == "Pokemon Fire Red", "numeric index query");
        Assert(CatalogCore.QueryTitle("10 Second Ninja X", "PC (Windows)") == "10 Second Ninja X", "preserve actual numbered PC title");
        Assert(CatalogCore.QueryTitle("Cladun X 2 PSP", "Sony PlayStation Portable") == "ClaDun x2", "PSP alias");
        Assert(CatalogCore.QueryTitle("Mega Man Xtreme 2 PSP", "Nintendo Game Boy Advance").EndsWith("PSP"), "preserve suffix on wrong platform");
        Assert(CatalogCore.PlatformKey("Nintendo Game Boy Advance") == "gba", "GBA mapping");
        Assert(CatalogCore.PlatformKey("Nintendo Game Boy Color") == "gbc", "GBC mapping distinct");
        var input = new CatalogInput { Name="01 Pokemon Fire Red", Platform="Nintendo Game Boy Advance" };
        var good = new CatalogGame { Id=1, Name="Pokémon FireRed Version", Platforms=new List<string>{"Game Boy Advance"} };
        var bad = new CatalogGame { Id=2, Name="Pokémon Red Version", Platforms=new List<string>{"Game Boy"} };
        var match = CatalogCore.Choose(input,new [] { bad,good });
        Assert(match.Game.Id == 1 && match.PlatformMatches,"Pokemon correct GBA candidate ranks first");
        Assert(match.MatchStatus == "Strong candidate", "Pokemon high candidate with corroboration");
        var wrong = CatalogCore.Choose(new CatalogInput{Name="Pokemon Diamond",Platform="Nintendo Game Boy Color"},
            new[]{ new CatalogGame {Id=3,Name="Pokémon Diamond", Platforms=new List<string>{"Nintendo DS"}} });
        Assert(wrong.MatchStatus == "Review", "platform mismatch prevents auto-eligibility");
        var ambig = CatalogCore.Choose(new CatalogInput{Name="Golden Sun",Platform="Nintendo Game Boy Advance"},
            new[]{new CatalogGame {Id=5,Name="Golden Sun",Platforms=new List<string>{"Game Boy Advance"}},
                   new CatalogGame {Id=6,Name="Golden Sun",Platforms=new List<string>{"Game Boy Advance"}} });
        Assert(ambig.MatchStatus == "Review", "tied candidates require review");
        var leaf = CatalogCore.QueryVariants("02 Pokemon Leaf Green", "Nintendo Game Boy Advance");
        Assert(leaf.Any(x=>x.Text=="Pokemon LeafGreen Version"), "LeafGreen catalog-style search variant");
        Assert(leaf.Count<=4 && leaf[0].Text=="Pokemon Leaf Green", "bounded prioritized search variants");
        var fire=CatalogCore.QueryVariants("01 Pokemon Fire Red", "Nintendo Game Boy Advance");
        Assert(fire.Any(x=>x.Text=="Pokemon FireRed Version"), "FireRed version search variant");
        var mario=CatalogCore.QueryVariants("05 Super Mario Adv", "Nintendo Game Boy Advance");
        Assert(mario.Any(x=>x.Text=="Super Mario Advance" && x.StrongEvidence), "audited Super Mario Advance GBA alias");
        Assert(CatalogCore.QueryVariants("09 Yoshi's Island-Super Mario Advance 3", "Nintendo Game Boy Advance")
            .Any(x=>x.Text=="Super Mario Advance 3" && !x.StrongEvidence), "subtitle fallback search");
        Assert(CatalogCore.QueryVariants("10 Second Ninja X","PC (Windows)")[0].Text=="10 Second Ninja X", "preserve legitimate PC leading number");
        Assert(CatalogCore.PlatformId("Game Boy Advance")==24 && CatalogCore.PlatformId("PlayStation Portable")==38, "structured platform identifiers");
        var direct=CatalogCore.BuildSingleQueryBody("Pokemon FireRed Version");
        Assert(direct.Contains("search \"Pokemon FireRed Version\"") && direct.Contains("platforms.id") &&
            direct.Contains("first_release_date"), "direct search requests structured metadata");
        var inj=CatalogCore.BuildSingleQueryBody("Halo\"; where id = 1; //");
        Assert(!inj.Contains("//") && !inj.Contains("\"; where") && inj.Contains("search "),
            "untrusted title cannot inject APICalypse clauses");
        var otherPlatform=new CatalogGame {Id=40,Name="Pokemon Diamond",PlatformIds=new List<long>{20},Platforms=new List<string>{"Nintendo DS"}};
        Assert(CatalogCore.Choose(new CatalogInput{Name="Pokemon Diamond",Platform="Game Boy Color"},new[]{otherPlatform}).MatchStatus=="Review", "mismatched platform IDs never strong");
        var shortName=new CatalogGame {Id=41,Name="Mario Kart",PlatformIds=new List<long>{24},Platforms=new List<string>{"Game Boy Advance"}};
        Assert(CatalogCore.Choose(new CatalogInput{Name="06 Mario Kart",Platform="Game Boy Advance"},new[]{shortName}).MatchStatus=="Review", "generic short title must be reviewed");
        var dedup = CatalogCore.Choose(new CatalogInput{Name="Golden Sun",Platform="Game Boy Advance"},new [] {
            new CatalogGame{Id=99,Name="Golden Sun",PlatformIds=new List<long>{24}},
            new CatalogGame{Id=99,Name="Golden Sun",PlatformIds=new List<long>{24}}
        });
        Assert(dedup.CandidatesFound==1, "same IGDB ID counted only once");
        var truncated=CatalogCore.QueryVariants("02 Pokemon Leaf Green", "Game Boy Advance");
        Assert(truncated.Count<=4, "no unbounded per-game query expansion");
        Assert(CatalogCore.BuildSingleQueryBody("M")=="", "too-short queries are skipped");
        Assert(CatalogCore.BuildSingleQueryBody("Sonic Advance").Contains("limit 40"), "bounded direct search");
        Assert(CatalogCore.QueryTitle("04 Dragon Ball-Ad Adv", "Game Boy Advance")=="Dragon Ball: Advanced Adventure", "verified alias query doesn't affect displayed title");
        // v0.3.3 live-regression targets: exact titles, platform and genuine ambiguity.
        var gba="Nintendo Game Boy Advance";
        Assert(CatalogCore.QueryVariants("04 Pokemon-Yellow Version","Nintendo Game Boy Color")
            .Any(v=>v.Text=="Pokemon Yellow Version: Special Pikachu Edition"),"Yellow expanded catalog title search");
        Assert(CatalogCore.QueryVariants("16 The King Of Fighters EX1",gba)
            .Any(v=>v.Text=="The King of Fighters EX: Neo Blood" && !v.StrongEvidence),
            "EX1 verified catalog spelling is search-only");
        Assert(CatalogCore.QueryVariants("17 The king of the fighter EX2",gba)
            .Any(v=>v.Text=="The King of Fighters EX2: Howling Blood"),"EX2 subtitle search");
        Assert(CatalogCore.QueryVariants("06 Mario Kart",gba)
            .Any(v=>v.Text=="Mario Kart: Super Circuit" && !v.StrongEvidence),"Mario Kart fallback not automatic proof");
        Assert(CatalogCore.QueryVariants("06 Mario Kart",gba).Count<=4,"query budget retained");
        var kirby=CatalogCore.Choose(new CatalogInput{Name="11 Kirby And The Amazing Mirror",Platform=gba},
            new[]{new CatalogGame{Id=3721,Name="Kirby & the Amazing Mirror",PlatformIds=new List<long>{24},FirstReleaseYear=2004}});
        Assert(kirby.MatchStatus=="Strong candidate","and/ampersand is orthographic difference");
        var doctor=CatalogCore.Choose(new CatalogInput{Name="10 Dr Mario and Puzzle League",Platform=gba},
            new[]{new CatalogGame{Id=22549,Name="Dr. Mario & Puzzle League",PlatformIds=new List<long>{24},FirstReleaseYear=2005}});
        Assert(doctor.MatchStatus=="Strong candidate","Doctor Mario conjunction and punctuation");
        var sonic=CatalogCore.Choose(new CatalogInput{Name="14 Sonic Advance",Platform=gba},new[]{
            new CatalogGame{Id=6283,Name="Sonic Advance",PlatformIds=new List<long>{24},FirstReleaseYear=2001},
            new CatalogGame{Id=6598,Name="Sonic Advance 2",PlatformIds=new List<long>{24},FirstReleaseYear=2002}});
        Assert(sonic.Game.Id==6283 && sonic.MatchStatus=="Strong candidate","sequel must not suppress unique exact GBA match");
        var yoshi=CatalogCore.Choose(new CatalogInput{Name="09 Yoshi's Island-Super Mario Advance 3",Platform=gba},new[]{
            new CatalogGame{Id=16747,Name="Yoshi's Island: Super Mario Advance 3",PlatformIds=new List<long>{24},FirstReleaseYear=2002},
            new CatalogGame{Id=16748,Name="Super Mario Advance 4: Super Mario Bros. 3",PlatformIds=new List<long>{24},FirstReleaseYear=2003}});
        Assert(yoshi.Game.Id==16747 && yoshi.MatchStatus=="Strong candidate","combined exact GBA title beats different numbered sequel");
        var duplicate=CatalogCore.Choose(new CatalogInput{Name="14 Sonic Advance",Platform=gba},new[]{
            new CatalogGame{Id=6283,Name="Sonic Advance",PlatformIds=new List<long>{24},FirstReleaseYear=2001},
            new CatalogGame{Id=6284,Name="Sonic Advance",PlatformIds=new List<long>{24},FirstReleaseYear=2001}});
        Assert(duplicate.MatchStatus=="Review","distinct exact same-platform IGDB records need review");
        var kart=CatalogCore.Choose(new CatalogInput{Name="06 Mario Kart",Platform=gba},new[]{
            new CatalogGame{Id=221269,Name="Mario Kart",PlatformIds=new List<long>{6},FirstReleaseYear=2019},
            new CatalogGame{Id=2343,Name="Mario Kart: Super Circuit",PlatformIds=new List<long>{24},FirstReleaseYear=2001}});
        Assert(kart.Game.Id==2343 && kart.MatchStatus=="Review","correct-platform Mario Kart candidate ranked but not auto-approved");
        // v0.4.26: CSV audit of 318 titles found IGDB alias-only false-strength.
        // These are legitimate catalog search results, but different canonical
        // title/edition strings must not be approved from an alias alone.
        foreach(var pairing in new[]{
            Tuple.Create("70 Bump Bomb","Flame Beetles"),
            Tuple.Create("82 Manic Troll","Coin Digger"),
            Tuple.Create("74 Hanoic Tower","Mystic Totem")})
        {
            var aliasOnly=CatalogCore.Choose(new CatalogInput{Name=pairing.Item1,Platform=gba},
                new[]{new CatalogGame{Id=246318,Name=pairing.Item2,
                    Aliases=new List<string>{CatalogCore.QueryTitle(pairing.Item1,gba)},
                    PlatformIds=new List<long>{24},FirstReleaseYear=2010}});
            Assert(aliasOnly.MatchStatus=="Review" && aliasOnly.AliasOnlyUnrelatedTitle &&
                !CatalogCore.CanAutoPlan(aliasOnly),
                "unrelated official JungleTac names cannot inherit trusted alias authority: "+pairing.Item1);
        }
        var anguna=CatalogCore.Choose(new CatalogInput{Name="67 Anguna",Platform=gba},
            new[]{new CatalogGame{Id=7001,Name="Anguna: Warriors of Virtue",
                Aliases=new List<string>{"Anguna"},PlatformIds=new List<long>{24},FirstReleaseYear=2008}});
        Assert(anguna.MatchStatus=="Strong candidate" && !anguna.AliasOnlyUnrelatedTitle,
            "clear official-title prefix is not penalized as an unrelated alias");
        foreach(var name in new[]{"Human: Fall Flat - Legacy", "Nightingale - Legacy Mode",
            "Monster Hunter Wilds Beta test", "PlanetSide 2 - Test", "Trackmania Starter Access"})
        {
            var options=CatalogCore.QueryVariants(name,"PC (Windows)");
            Assert(options.Count<=4 && options.Skip(1).Any(x=>!x.StrongEvidence),
                "install/legacy/beta suffix adds search-only discovery: "+name);
        }
        Assert(CatalogCore.QueryVariants("Ys One And Two PSP","Sony PlayStation Portable")
            .Any(x=>x.Text=="Ys I & II Chronicles"&&!x.StrongEvidence),
            "PSP Ys I and II official packaging lookup is search-only");
        Assert(CatalogCore.QueryVariants("Besiege + The Splintered Sea DLC","PC (Windows)")
            .Any(x=>x.Text=="Besiege"&&!x.StrongEvidence),
            "DLC base-title lookup does not assert edition identity");
        var beta=CatalogCore.Choose(new CatalogInput{Name="Monster Hunter Wilds Beta test",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=9998,Name="Monster Hunter Wilds Beta test",
                PlatformIds=new List<long>{6},FirstReleaseYear=2025}});
        Assert(beta.MatchStatus=="Review" && !CatalogCore.CanAutoPlan(beta),
            "even exact beta/launcher/legacy naming needs manual identity approval");
        var ff2=CatalogCore.Choose(new CatalogInput{Name="Final Fantasy 2",Platform="Sony PlayStation Portable"},
            new[]{new CatalogGame{Id=21779,Name="Final Fantasy IV: The Complete Collection",
                Aliases=new List<string>{"Final Fantasy 2"},PlatformIds=new List<long>{38},FirstReleaseYear=2011},
                new CatalogGame{Id=121717,Name="Final Fantasy II: 20th Anniversary Edition",
                Aliases=new List<string>{"Final Fantasy 2"},PlatformIds=new List<long>{38},FirstReleaseYear=2007}});
        Assert(ff2.Game.Id==121717 && ff2.MatchStatus=="Review",
            "canonical edition relevance breaks tied aliases, but duplicate exact matches still need review");
        // RC3: regressions from the 837-row private RC2 library export.
        var hogwarts=CatalogCore.Choose(new CatalogInput{Name="Hogwarts Legacy",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=136625,Name="Hogwarts Legacy",PlatformIds=new List<long>{6},FirstReleaseYear=2023}});
        Assert(hogwarts.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(hogwarts),
            "a real game called Hogwarts Legacy is not an edition qualifier");
        Assert(!CatalogCore.QueryVariants("Hogwarts Legacy","PC (Windows)").Any(q=>q.Text=="Hogwarts"),
            "do not waste a broad IGDB search by stripping a real game's final word");
        var falconeer=CatalogCore.Choose(new CatalogInput{Name="The Falconeer",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=121709,Name="The Falconeer: Revolution Remaster",
                Aliases=new List<string>{"The Falconeer"},PlatformIds=new List<long>{6},FirstReleaseYear=2020}});
        Assert(falconeer.EditionConflict && falconeer.MatchStatus=="Review" && !CatalogCore.CanAutoPlan(falconeer),
            "original game and remaster cannot silently share a strong identity");
        var exalt=CatalogCore.Choose(new CatalogInput{Name="Realm of the Mad God Exalt",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=8188,Name="Realm of the Mad God",
                Aliases=new List<string>{"Realm of the Mad God Exalt"},PlatformIds=new List<long>{6},FirstReleaseYear=2011}});
        Assert(exalt.EditionConflict && exalt.MatchStatus=="Review" && !CatalogCore.CanAutoPlan(exalt),
            "Exalt client and base-game identity require review");
        var authenticEdition=CatalogCore.Choose(new CatalogInput{Name="Sample Remastered",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=801,Name="Sample Remastered",PlatformIds=new List<long>{6},FirstReleaseYear=2025}});
        Assert(authenticEdition.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(authenticEdition),
            "matching edition labels on both sides are safe");
        var finals=CatalogCore.Choose(new CatalogInput{Name="THE FINALS",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=146022,Name="Final Fantasy IV: The After Years",
                PlatformIds=new List<long>{6},FirstReleaseYear=2013}});
        Assert(finals.MatchStatus=="Unmatched" && finals.Game==null,
            "unrelated Final Fantasy candidate must not appear as a recommended match for THE FINALS");
        Assert(CatalogCore.QueryVariants("THE FINALS","PC (Windows)").Any(q=>q.Text=="FINALS"&&!q.StrongEvidence),
            "article-free discovery may locate the correct Finals catalog identity");
        var trueFinals=CatalogCore.Choose(new CatalogInput{Name="THE FINALS",Platform="PC (Windows)"},
            new[]{new CatalogGame{Id=909091,Name="The Finals",PlatformIds=new List<long>{6},FirstReleaseYear=2023}});
        Assert(trueFinals.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(trueFinals),
            "the genuine Finals game remains a strict-ready match");
        Assert(CatalogCore.QueryVariants("Final Fantasy X-X2 - HD Remaster","PC (Windows)")
            .Any(q=>q.Text.Contains("X/X-2")&&!q.StrongEvidence),
            "combined Final Fantasy title can search the storefront's slash notation");
        Assert(CatalogCore.QueryVariants("Tomb Raider I-III Remastered Starring Lara Croft","PC (Windows)")
            .Any(q=>q.Text=="Tomb Raider I-III Remastered"&&!q.StrongEvidence),
            "remastered collection searches a shorter catalog variant without asserting identity");
        // RC4: preserve plausible same-platform matches for human review only.
        var partialReviews=new[]{
            Tuple.Create("26 Astro Boy","Nintendo Game Boy Advance","Astro Boy: Omega Factor",24L),
            Tuple.Create("44 Lights, Camera, Pants!","Nintendo Game Boy Advance",
                "SpongeBob SquarePants: Lights, Camera, Pants!",24L),
            Tuple.Create("53 Jackie Chan Adv","Nintendo Game Boy Advance",
                "Jackie Chan Adventures: Legend of the Dark Hand",24L),
            Tuple.Create("64 Mission Impossible","Nintendo Game Boy Advance",
                "Mission: Impossible - Operation Surma",24L),
            Tuple.Create("Final Fantasy Dissidia PSP","Sony PlayStation Portable",
                "Dissidia Final Fantasy",38L),
            Tuple.Create("Lunar - Genesis","Nintendo DS","Lunar Genesis: Dragon Song",20L),
            Tuple.Create("Spectral Souls","Sony PlayStation Portable",
                "Spectral Souls: Resurrection of the Ethereal Empires",38L)
        };
        long rcId=910000;
        foreach(var pair in partialReviews)
        {
            var candidate=CatalogCore.Choose(new CatalogInput{Name=pair.Item1,Platform=pair.Item2},
                new[]{new CatalogGame{Id=rcId++,Name=pair.Item3,
                    PlatformIds=new List<long>{pair.Item4},FirstReleaseYear=2004}});
            Assert(candidate.MatchStatus=="Review" && candidate.Game!=null &&
                !CatalogCore.CanAutoPlan(candidate),
                "abbreviated/partial same-platform identity remains visible as manual Review: "+pair.Item1);
        }
        var badOnly=CatalogCore.Choose(new CatalogInput{Name="84 On N0 Off",Platform=gba},
            new[]{new CatalogGame{Id=rcId++,Name="Blopy Ventures N0",
                PlatformIds=new List<long>{24},FirstReleaseYear=2001}});
        Assert(badOnly.MatchStatus=="Unmatched" && badOnly.Game==null,
            "unrelated same-platform candidate remains suppressed");
        var platformDisagreement=CatalogCore.Choose(
            new CatalogInput{Name="Spectral Souls",Platform="Sony PlayStation Portable"},
            new[]{new CatalogGame{Id=rcId++,Name="Spectral Souls: Resurrection of the Ethereal Empires",
                PlatformIds=new List<long>{8},FirstReleaseYear=2004}});
        Assert(platformDisagreement.MatchStatus!="Strong candidate" &&
            !CatalogCore.CanAutoPlan(platformDisagreement),
            "wrong-platform partial game is never strong or auto ready");
        Assert(CatalogCore.QueryVariants("Human: Fall Flat Legacy","PC (Windows)")
            .Any(v=>v.Text=="Human: Fall Flat" && !v.StrongEvidence),
            "multiword Legacy client strips to base title only for discovery");
        Assert(!CatalogCore.QueryVariants("Hogwarts Legacy","PC (Windows)")
            .Any(v=>v.Text=="Hogwarts"),
            "two-word normal title does not receive dangerous broad Legacy search");
        var ffVariants=CatalogCore.QueryVariants("Final Fantasy X-X2 - HD Remaster","PC (Windows)");
        Assert(ffVariants.Count<=4 && ffVariants.Any(v=>
            v.Text=="Final Fantasy X/X-2 HD Remaster" && !v.StrongEvidence),
            "combined collection query contains canonical slash and no redundant HD separator");
        Assert(CatalogCore.BuildSingleQueryBody("Final Fantasy X/X-2 HD Remaster")
            .Contains("search \"Final Fantasy X/X-2 HD Remaster\""),
            "slash survives safe IGDB query serialization");
        Assert(CatalogCore.BuildExactNameQueryBody("THE FINALS")
            .Contains("where name ~ \"THE FINALS\"; limit 10;"),
            "bounded case-insensitive exact-name fallback prepared");
        Assert(CatalogCore.BuildExactNameQueryBody("THE FINALS\"; where id = 1;")=="",
            "exact-name fallback refuses injected APICalypse punctuation");
        Assert(CatalogCore.BuildExactNameQueryBody("Hi")=="",
            "exact-name fallback refuses too-short titles");
        // Same-platform partial titles must never be granted strict import rights
        // simply because the provider reports a large relevance score.
        Assert(!CatalogCore.CanAutoPlan(CatalogCore.Choose(
            new CatalogInput{Name="Spectral Souls",Platform="Sony PlayStation Portable"},
            new[]{new CatalogGame{Id=rcId++,Name="Spectral Souls: Resurrection of the Ethereal Empires",
                PlatformIds=new List<long>{38},FirstReleaseYear=2006}})),
            "manual-only anchor is not an unattended write authorization");
        // v0.4.7: only exact, uniquely identified, dated, structured-platform
        // catalog matches may be included in the automatic planning queue.
        Assert(CatalogCore.CanAutoPlan(sonic),"strict auto gate accepts unique exact dated platform-ID match");
        sonic.Reason="Localized UI prose is free to change";
        Assert(CatalogCore.CanAutoPlan(sonic),"typed strict evidence survives displayed text changes");
        Assert(CatalogCore.CanAutoPlan(kirby),"orthographic and/ampersand alias remains eligible");
        Assert(!CatalogCore.CanAutoPlan(duplicate),"duplicate exact IGDB identities rejected from auto plan");
        Assert(!CatalogCore.CanAutoPlan(kart),"abbreviated Mario Kart remains manual review");
        Assert(!CatalogCore.CanAutoPlan(wrong),"wrong-platform result never auto planned");
        var noYear=CatalogCore.Choose(new CatalogInput{Name="Sonic Advance",Platform=gba},
            new[]{new CatalogGame{Id=6283,Name="Sonic Advance",PlatformIds=new List<long>{24}}});
        Assert(!CatalogCore.CanAutoPlan(noYear),"undated game remains supervised");
        var nameOnly=CatalogCore.Choose(new CatalogInput{Name="Sonic Advance",Platform=gba},
            new[]{new CatalogGame{Id=6283,Name="Sonic Advance",Platforms=new List<string>{"Game Boy Advance"},FirstReleaseYear=2001}});
        Assert(!CatalogCore.CanAutoPlan(nameOnly),"platform name without structured ID not auto approved");
        Assert(!CatalogCore.CanAutoPlan(null),"null match refused");
        var shortDriv3r=CatalogCore.Choose(new CatalogInput{Name="18 Driv3r",Platform=gba},new[]{
            new CatalogGame{Id=194978,Name="Driv3r",PlatformIds=new List<long>{24},FirstReleaseYear=2005},
            new CatalogGame{Id=5817,Name="Driv3r",PlatformIds=new List<long>{7},FirstReleaseYear=2004}});
        Assert(shortDriv3r.Game.Id==194978 && shortDriv3r.MatchStatus=="Strong candidate","distinctive short title with dated same-platform release");
        var fireDragon=CatalogCore.Choose(new CatalogInput{Name="09 Fire Dragon",Platform="Nintendo Game Boy"},
            new[]{new CatalogGame{Id=247297,Name="Fire Dragon",PlatformIds=new List<long>{33}}});
        Assert(fireDragon.MatchStatus=="Review","undated short title without catalog context stays review");
        var contextualFire=CatalogCore.Choose(new CatalogInput{Name="09 Fire Dragon",Platform="Nintendo Game Boy"},
            new[]{new CatalogGame{Id=247297,Name="Fire Dragon",PlatformIds=new List<long>{33},
                Summary="Game Boy edition of the unlicensed Fire Dragon game, published by Gowin for the handheld system."}});
        Assert(contextualFire.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(contextualFire),
            "unique exact undated Game Boy title with platform ID and context is eligible for approved import");
        var wrongFire=CatalogCore.Choose(new CatalogInput{Name="09 Fire Dragon",Platform="Nintendo Game Boy"},
            new[]{new CatalogGame{Id=247297,Name="Fire Dragon",PlatformIds=new List<long>{18},
                Summary="Game Boy edition of the unlicensed Fire Dragon game, published by Gowin for the handheld system."}});
        Assert(wrongFire.MatchStatus=="Review" && !CatalogCore.CanAutoPlan(wrongFire),
            "descriptive text cannot override mismatched structured platform ID");
        var duplicatedFire=CatalogCore.Choose(new CatalogInput{Name="09 Fire Dragon",Platform="Nintendo Game Boy"},
            new[]{new CatalogGame{Id=247297,Name="Fire Dragon",PlatformIds=new List<long>{33},
                    Summary="Game Boy edition of the unlicensed Fire Dragon game, published by Gowin for the handheld system."},
                  new CatalogGame{Id=247298,Name="Fire Dragon",PlatformIds=new List<long>{33},
                    Summary="Another catalog record for Fire Dragon, same Game Boy system, which must remain ambiguous."}});
        Assert(duplicatedFire.MatchStatus=="Review" && !CatalogCore.CanAutoPlan(duplicatedFire),
            "multiple exact same-platform IDs still require manual review even with descriptions");
        var abbreviated=CatalogCore.Choose(new CatalogInput{Name="05 Super Mario Adv",Platform=gba},
            new[]{new CatalogGame{Id=81197,Name="Super Mario Advance",PlatformIds=new List<long>{24},FirstReleaseYear=2001}});
        Assert(abbreviated.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(abbreviated),
            "audited GBA Super Mario Advance alias with unique exact platform ID and year");
        Assert(CatalogCore.QueryTitle("05 Super Mario Adv","Game Boy Color")=="Super Mario Adv",
            "GBA-specific abbreviation does not affect other platforms");
        Assert(CatalogCore.QueryVariants("53 Jackie Chan Adv",gba)
            .Any(v=>v.Text=="Jackie Chan Adventures" && !v.StrongEvidence),
            "ambiguous Adv plural expansion is search-only");
        Assert(CatalogCore.QueryVariants("22 Naruto Ninja Council 1",gba)
            .Any(v=>v.Text=="Naruto Ninja Council" && !v.StrongEvidence),
            "collection's informal first-game number is search-only");
        Assert(CatalogCore.QueryVariants("Untold Legends 2 - The Warriors Code","Sony PlayStation Portable")
            .Any(v=>v.Text=="Untold Legends: The Warriors Code" && !v.StrongEvidence),
            "series number removal is discovery only");
        Assert(CatalogCore.QueryTitle("WDL - World Destruction League - Thunder Tanks","Game Boy Color")
            == "World Destruction League - Thunder Tanks","redundant, verified acronym removed");
        Assert(CatalogCore.QueryTitle("WDL - Weird Dragon Factory","Game Boy Color")
            == "WDL - Weird Dragon Factory","nonmatching acronym preserved");
        var wdl = CatalogCore.Choose(new CatalogInput{Name="WDL - World Destruction League - Thunder Tanks",
            Platform="Game Boy Color"},new[]{new CatalogGame{Id=17,Name="World Destruction League: Thunder Tanks",
            PlatformIds=new List<long>{22},FirstReleaseYear=2000}});
        Assert(wdl.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(wdl),
            "identical expansion with platform, date and unique title is strict-ready");
        var advancedTwo=CatalogCore.Choose(new CatalogInput{Name="Super Mario Advance 2 - Super Mario World",
            Platform=gba},new[]{new CatalogGame{Id=16617,Name="Super Mario World: Super Mario Advance 2",
            PlatformIds=new List<long>{24},FirstReleaseYear=2001},
            new CatalogGame{Id=16748,Name="Super Mario Advance 4: Super Mario Bros. 3",
            PlatformIds=new List<long>{24},FirstReleaseYear=2003}});
        Assert(advancedTwo.Game.Id==16617 && advancedTwo.MatchStatus=="Strong candidate" &&
            CatalogCore.CanAutoPlan(advancedTwo),"reordered complete subtitle retains distinct installment number");
        var advancedThree=CatalogCore.Choose(new CatalogInput{Name="Super Mario Advance 3 - Yoshi's Island",
            Platform=gba},new[]{new CatalogGame{Id=16747,Name="Yoshi's Island: Super Mario Advance 3",
            PlatformIds=new List<long>{24},FirstReleaseYear=2002}});
        Assert(advancedThree.MatchStatus=="Strong candidate" && CatalogCore.CanAutoPlan(advancedThree),
            "reordered title preserves possessive and numeral");
        var unrelatedNumber=CatalogCore.Choose(new CatalogInput{Name="Super Mario Advance 2 - Super Mario World",
            Platform=gba},new[]{new CatalogGame{Id=16747,Name="Super Mario World: Super Mario Advance 3",
            PlatformIds=new List<long>{24},FirstReleaseYear=2002}});
        Assert((unrelatedNumber.MatchStatus=="Review" || unrelatedNumber.MatchStatus=="Unmatched") &&
            !CatalogCore.CanAutoPlan(unrelatedNumber),
            "wrong sequel numeral cannot be promoted by reordering");
        var ambiguousWdl=CatalogCore.Choose(new CatalogInput{Name="WDL - World Destruction League - Thunder Tanks",
            Platform="Game Boy Color"},new[]{new CatalogGame{Id=17,Name="World Destruction League: Thunder Tanks",
            PlatformIds=new List<long>{22},FirstReleaseYear=2000},
            new CatalogGame{Id=18,Name="World Destruction League: Thunder Tanks",
            PlatformIds=new List<long>{22},FirstReleaseYear=2000}});
        Assert(ambiguousWdl.MatchStatus=="Review" && !CatalogCore.CanAutoPlan(ambiguousWdl),
            "two exact same-platform IDs still require review");
        Console.WriteLine("PASS: " + checks + " offline catalog matching checks; no network or Playnite access.");
    }
}
