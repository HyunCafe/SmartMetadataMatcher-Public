using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class EnrichmentPlanTests
{
    private static int count;
    private static void Check(bool value,string label)
    {
        count++;
        if(!value) throw new Exception("Enrichment test failed: "+label);
    }
    internal static void Run()
    {
        Check(EnrichmentPlan.BatchEligible("Strong candidate",false,"1516"),"marked strong candidate eligible for supervised batch");
        Check(!EnrichmentPlan.BatchEligible("Review",false,"1516"),"review candidates must not batch apply");
        Check(!EnrichmentPlan.BatchEligible("Strong candidate",true,"1516"),"completed candidates must not batch apply");
        Check(!EnrichmentPlan.BatchEligible("Strong candidate",false,"bad"),"invalid IGDB IDs must not batch apply");
        Check(EnrichmentPlan.IdentityKey("Pokémon - LeafGreen Version") ==
            EnrichmentPlan.IdentityKey("Pokemon LeafGreen Version"),"collision normalization");
        var own=Guid.NewGuid();
        var games=new List<Tuple<Guid,string,string>> {
            Tuple.Create(own,"02 Pokemon Leaf Green","Nintendo Game Boy Advance"),
            Tuple.Create(Guid.NewGuid(),"Pokemon - LeafGreen Version","Nintendo Game Boy Advance"),
            Tuple.Create(Guid.NewGuid(),"Pokémon LeafGreen Version","Nintendo DS")
        };
        Check(EnrichmentPlan.HasCollision("Pokémon LeafGreen Version",own,"Nintendo Game Boy Advance",games),
            "same-platform title collision blocked");
        games.RemoveAt(1);
        Check(!EnrichmentPlan.HasCollision("Pokémon LeafGreen Version",own,"Nintendo Game Boy Advance",games),
            "different platform is not same-platform collision");
        Check(EnrichmentPlan.IsSafeTitle("Pokémon LeafGreen Version"),"Unicode title accepted");
        Check(!EnrichmentPlan.IsSafeTitle("\nBAD"),"title newline rejected");
        Check(EnrichmentPlan.MaySetSorting("", "Old") &&
            EnrichmentPlan.MaySetSorting("Old", "Old") &&
            !EnrichmentPlan.MaySetSorting("Special order","Old"),"custom sorting preserved");
        var op1=Guid.NewGuid();var op2=Guid.NewGuid();var op3=Guid.NewGuid();
        var undoHistory=new[]{op3,op2,op1};
        Check(EnrichmentPlan.SuggestedDisplayTitle("02 Pokemon Leaf Green", "Pokémon LeafGreen Version") ==
            "Pokemon Leaf Green", "number prefix cleaned without unwanted Version suffix");
        Check(EnrichmentPlan.SuggestedDisplayTitle("05 Super Mario Adv", "Super Mario Advance") ==
            "Super Mario Advance", "approved catalog title expands abbreviation");
        var gbaId=Guid.NewGuid();
        var gbaGames=new List<Tuple<Guid,string,string>>{
            Tuple.Create(gbaId,"05 Super Mario Adv","Nintendo Game Boy Advance"),
            Tuple.Create(Guid.NewGuid(),"Super Mario Advance","Nintendo Game Boy Advance"),
            Tuple.Create(Guid.NewGuid(),"Super Mario Advance","Nintendo DS")};
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("05 Super Mario Adv","Super Mario Advance",
            gbaId,"Nintendo Game Boy Advance",gbaGames)=="Super Mario Advance (GBA)",
            "same-platform duplicate gets canonical catalog title plus GBA suffix");
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("05 Super Mario Adv","Super Mario Advance",
            gbaId,"Nintendo DS",gbaGames)=="Super Mario Advance (NDS)",
            "platform tag based on requested platform");
        var crossPlatform=new[]{Tuple.Create(Guid.NewGuid(),"Donkey Kong","Nintendo Game Boy")};
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("04 Donkey Kong","Donkey Kong",
            gbaId,"Nintendo Game Boy Advance",crossPlatform)=="Donkey Kong (GBA)",
            "different-console duplicate title uses GBA label automatically");
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("04 Donkey Kong","Donkey Kong",
            gbaId,"Nintendo Game Boy Advance",new[]{
                Tuple.Create(Guid.NewGuid(),"Donkey Kong","Nintendo Game Boy Advance")})=="Donkey Kong (GBA)",
            "same-console duplicate title uses GBA label automatically");
        gbaGames.Add(Tuple.Create(Guid.NewGuid(),"Super Mario Advance (GBA)","Nintendo Game Boy Advance"));
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("05 Super Mario Adv","Super Mario Advance",
            gbaId,"Nintendo Game Boy Advance",gbaGames)=="",
            "second collision does not manufacture ambiguous names");
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("02 Pokemon Leaf Green","Pokémon LeafGreen Version",
            gbaId,"Nintendo Game Boy Advance",new Tuple<Guid,string,string>[0])=="Pokemon Leaf Green",
            "cleaned Pokemon name is retained without collision");
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("02 Pokemon Leaf Green","Pokémon LeafGreen Version",
            gbaId,"Nintendo Game Boy Advance",new[]{
                Tuple.Create(Guid.NewGuid(),"Pokemon Leaf Green","Nintendo Game Boy Advance")})=="Pokemon Leaf Green (GBA)",
            "Pokemon duplicate gets readable GBA suffix instead of retaining numeric prefix");
        Check(EnrichmentPlan.SuggestedUniqueDisplayTitle("Super Mario Advance","Super Mario Advance",
            gbaId,"Nintendo Game Boy Advance",new Tuple<Guid,string,string>[0])=="Super Mario Advance",
            "already-clean title stays unchanged");
        var visibilityDir=Path.Combine(Path.GetTempPath(),"SMM-history-visibility-"+Guid.NewGuid().ToString("N"));
        try{
            var visibility=new HistoryVisibilityStore(visibilityDir);
            Check(visibility.Load().Count==0,"empty history visibility preferences");
            var hideId=Guid.NewGuid();
            visibility.Save(new[]{hideId,hideId});
            Check(visibility.Load().SetEquals(new[]{hideId}),"hidden IDs persist and deduplicate");
            visibility.Save(new Guid[0]);
            Check(visibility.Load().Count==0,"hidden IDs restore without touching journal");
            var file=Path.Combine(visibilityDir,"history-hidden-operations.txt");
            File.WriteAllText(file,"CORRUPT\n");
            bool rejected=false;
            try{visibility.Load();}catch(InvalidDataException){rejected=true;}
            Check(rejected,"malformed visibility file rejected without changing journal");
        }
        finally{if(Directory.Exists(visibilityDir))Directory.Delete(visibilityDir,true);}
        Check(EnrichmentPlan.SuggestedDisplayTitle("007 Nightfire", "007 Nightfire") ==
            "007 Nightfire", "three digit identifiers preserved");
        Check(EnrichmentPlan.IsLatestUndoPrefix(undoHistory,new[]{op3}),
            "newest operation can be selected for undo");
        Check(EnrichmentPlan.IsLatestUndoPrefix(undoHistory,new[]{op2,op3}),
            "newest two can be selected regardless of UI order");
        Check(!EnrichmentPlan.IsLatestUndoPrefix(undoHistory,new[]{op2}),
            "older noncontiguous undo blocked");
        Check(!EnrichmentPlan.IsLatestUndoPrefix(undoHistory,new[]{op3,op1}),
            "skipping an intermediate operation blocked");
        Check(!EnrichmentPlan.IsLatestUndoPrefix(undoHistory,new[]{op3,op3}),
            "duplicate operation selection blocked");
        var dir=Path.Combine(Path.GetTempPath(),"SMM-enrichment-test-"+Guid.NewGuid().ToString("N"));
        try
        {
            var j=new EnrichmentJournal(dir);
            Check(j.LatestOpen()==null && j.Committed().Count==0,"new journal is empty");
            var entry=new EnrichmentEntry {
                OperationId=Guid.NewGuid(),GameId=Guid.NewGuid(),IgdbId=1516,
                CatalogTitle="Pokémon LeafGreen Version",BeforeName="02 Pokemon Leaf Green",
                AfterName="Pokémon LeafGreen Version", ChangedName=true,
                BeforeDescription="",AfterDescription="<p>Pokémon &amp; friends</p>",ChangedDescription=true,
                BeforeCover="",AfterCover="new-cover",ChangedCover=true,
                BeforeIcon="",AfterIcon="new-icon",ChangedIcon=true,
                BeforeBackground="",AfterBackground="new-background",ChangedBackground=true,
                BeforeSorting="Original Custom",AfterSorting="Original Custom",Note="test"
            };
            var genreId=Guid.NewGuid();var developerId=Guid.NewGuid();
            entry.BeforeRelease=""; entry.AfterRelease="2005"; entry.ChangedRelease=true;
            entry.BeforeGenres=new List<Guid>();entry.AfterGenres=new List<Guid>{genreId};entry.ChangedGenres=true;
            entry.BeforeDevelopers=new List<Guid>();entry.AfterDevelopers=new List<Guid>{developerId};entry.ChangedDevelopers=true;
            j.Prepare(entry);
            var pending=j.LatestOpen();
            Check(pending!=null && !pending.Committed && pending.AfterName==entry.AfterName,
                "durable prepared entry round trip");
            Check(EnrichmentPlan.IsFullyApplied(pending,pending.AfterName,pending.AfterSorting,
                pending.AfterDescription,pending.AfterCover,pending.AfterIcon,pending.AfterBackground),
                "committed fields match live database");
            Check(EnrichmentPlan.ExtraFieldsApplied(pending,"2005",new List<Guid>{genreId},
                new List<Guid>{developerId},new List<Guid>(),new List<Guid>()),
                "new metadata IDs and release year round-trip in journal");
            Check(!EnrichmentPlan.ExtraFieldsApplied(pending,"2006",new List<Guid>{genreId},
                new List<Guid>{developerId},new List<Guid>(),new List<Guid>()),
                "changed release not mistaken for completed");
            Check(EnrichmentPlan.ExtraFieldsUndoSafe(pending,"2005",new List<Guid>{genreId},
                new List<Guid>(),new List<Guid>(),new List<Guid>()),
                "partial interrupted metadata update remains undoable");
            Check(!EnrichmentPlan.ExtraFieldsUndoSafe(pending,"2005",new List<Guid>{Guid.NewGuid()},
                new List<Guid>(),new List<Guid>(),new List<Guid>()),
                "manual edit of shared reference blocks undo");
            Check(EnrichmentPlan.SameIds(new List<Guid>{developerId,genreId},
                new List<Guid>{genreId,developerId}),"reference order not material");
            Check(!EnrichmentPlan.IsFullyApplied(pending,pending.BeforeName,pending.AfterSorting,
                pending.AfterDescription,pending.AfterCover,pending.AfterIcon,pending.AfterBackground),
                "changed live game not treated as completed");
            Check(EnrichmentPlan.CanUndo(pending,pending.AfterName,pending.AfterSorting,
                pending.AfterDescription,pending.AfterCover,pending.AfterIcon,pending.AfterBackground),
                "complete update can be undone");
            Check(EnrichmentPlan.CanUndo(pending,pending.BeforeName,pending.BeforeSorting,
                pending.AfterDescription,pending.BeforeCover,pending.AfterIcon,pending.BeforeBackground),
                "interrupted partial update can be restored");
            Check(!EnrichmentPlan.CanUndo(pending,"manual custom title",pending.AfterSorting,
                pending.AfterDescription,pending.AfterCover,pending.AfterIcon,pending.AfterBackground),
                "newer user edit blocks undo");
            Check(entry.PreparedUtc>new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),
                "new prepared operation has UTC date");
            j.Commit(entry.OperationId);
            Check(j.Committed().Count==1 && j.LatestOpen().Committed,"commit survived reload");
            Check(j.AllEntries().Single().CommittedUtc>=entry.PreparedUtc,
                "new commit records UTC timestamp");
            j.MarkUndone(entry.OperationId);
            Check(j.Committed().Count==0 && j.LatestOpen()==null,"undo survives reload");
            var archived=j.AllEntries();
            Check(archived.Count==1 && archived[0].Committed && archived[0].Undone,
                "undone history retained while active undo remains closed");
            Check(archived[0].UndoneUtc>=archived[0].CommittedUtc,
                "undo timestamp persists across reload");
            // v0.4.14 legacy two-field markers remain readable and undoable.
            var legacyDir=Path.Combine(dir,"legacy-markers");
            var oldJournal=new EnrichmentJournal(legacyDir);
            var oldEntry=new EnrichmentEntry {OperationId=Guid.NewGuid(),GameId=Guid.NewGuid(),
                IgdbId=42,BeforeName="Old",AfterName="New",ChangedName=true};
            oldJournal.Prepare(oldEntry);
            var logPath=Path.Combine(legacyDir,"enrichment-journal-v2.log");
            File.AppendAllText(logPath,"C|"+oldEntry.OperationId.ToString("N")+"\n"+
                "U|"+oldEntry.OperationId.ToString("N")+"\n");
            Check(oldJournal.AllEntries().Count==1 && oldJournal.AllEntries()[0].Undone,
                "legacy two-field journal markers remain readable");
            Check(oldJournal.AllEntries()[0].CommittedUtc==default(DateTime) &&
                oldJournal.AllEntries()[0].UndoneUtc==default(DateTime),
                "missing legacy timestamps remain unknown");
            var legacy=new MetadataJournal(dir);
            Check(legacy.LatestPending()==null,"old journal preserved separately");
            var again=new EnrichmentEntry {OperationId=Guid.NewGuid(),GameId=Guid.NewGuid(),IgdbId=2,
                BeforeName="Before",AfterName="After",ChangedName=true};
            j.Prepare(again);
            File.AppendAllText(Path.Combine(dir,"enrichment-journal-v2.log"),"TRUNCATED\n");
            bool blocked=false;
            try {j.OpenEntries();}catch(InvalidDataException){blocked=true;}
            Check(blocked,"corrupt journal fails closed");
        }
        finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+count+" coordinated metadata/journal/collision safety checks.");
    }
}
