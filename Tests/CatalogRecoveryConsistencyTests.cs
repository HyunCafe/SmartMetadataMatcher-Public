using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogRecoveryConsistencyTests
{
    private static int checks;
    private static void Check(bool yes,string why)
    {
        checks++;
        if(!yes)throw new Exception("Recovery consistency test FAILED: "+why);
    }
    private static CatalogMediaStage Stage(Guid operation,Guid game,bool closed,string outcome)
    {return new CatalogMediaStage {OperationId=operation,GameId=game,Closed=closed,Outcome=outcome};}
    private static EnrichmentEntry Entry(Guid operation,Guid game,bool committed,bool undone=false)
    {return new EnrichmentEntry {OperationId=operation,GameId=game,IgdbId=123,Committed=committed,Undone=undone};}
    internal static void Run()
    {
        var op=Guid.NewGuid();var game=Guid.NewGuid();
        var empty=CatalogRecoveryConsistency.Inspect(new CatalogMediaStage[0],new EnrichmentEntry[0]);
        Check(empty.AllowsWrites,"empty installation");
        var legacy=CatalogRecoveryConsistency.Inspect(new CatalogMediaStage[0],
            new[]{Entry(op,game,true)});
        Check(legacy.AllowsWrites,"old committed operation without media ledger");
        var handoff=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"H")},
            new[]{Entry(op,game,true)});
        Check(handoff.AllowsWrites && handoff.HandOffs==1,"valid committed handoff");
        var undone=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"H")},
            new[]{Entry(op,game,true,true)});
        Check(undone.AllowsWrites,"undone journal still owns past handoff");
        var abandoned=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"A")},
            new EnrichmentEntry[0]);
        Check(abandoned.AllowsWrites && abandoned.CleanAbandons==1,"verified abandoned stage");
        var open=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,false,null)},
            new EnrichmentEntry[0]);
        Check(!open.AllowsWrites && open.OpenStages==1,"open media staging blocks");
        var orphan=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"H")},
            new EnrichmentEntry[0]);
        Check(!orphan.AllowsWrites && orphan.Problems[0].Contains("Orphan"),"orphan handoff blocks");
        var mismatch=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,Guid.NewGuid(),true,"H")},
            new[]{Entry(op,game,true)});
        Check(!mismatch.AllowsWrites,"mismatched game IDs block");
        // Cross-journal media ownership is checked for every field and exact ID.
        var correctStage=Stage(op,game,true,"H");
        correctStage.Media.Add("cover","media/cover-A");
        correctStage.Media.Add("icon","media/icon-A");
        correctStage.Media.Add("background","media/background-A");
        var correctEntry=Entry(op,game,true);
        correctEntry.ChangedCover=true;correctEntry.AfterCover="media/cover-A";
        correctEntry.ChangedIcon=true;correctEntry.AfterIcon="media/icon-A";
        correctEntry.ChangedBackground=true;correctEntry.AfterBackground="media/background-A";
        Check(CatalogRecoveryConsistency.Inspect(new[]{correctStage},new[]{correctEntry}).AllowsWrites,
            "all three imported artwork IDs match the journal");
        var wrongCover=Entry(op,game,true);
        wrongCover.ChangedCover=true;wrongCover.AfterCover="media/DIFFERENT";
        var oneCover=Stage(op,game,true,"H");oneCover.Media.Add("cover","media/cover-A");
        Check(!CatalogRecoveryConsistency.Inspect(new[]{oneCover},new[]{wrongCover}).AllowsWrites,
            "wrong cover ID blocks import");
        var missingCover=Entry(op,game,true);missingCover.ChangedCover=true;
        missingCover.AfterCover="media/cover-A";
        Check(!CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"H")},
            new[]{missingCover}).AllowsWrites,"missing cover staging marker blocks");
        Check(!CatalogRecoveryConsistency.Inspect(new[]{oneCover},new[]{Entry(op,game,true)}).AllowsWrites,
            "staged cover not owned by field journal blocks");
        var wrongIcon=Entry(op,game,true);wrongIcon.ChangedIcon=true;
        wrongIcon.AfterIcon="media/wrong";
        var oneIcon=Stage(op,game,true,"H");oneIcon.Media.Add("icon","media/right");
        Check(!CatalogRecoveryConsistency.Inspect(new[]{oneIcon},new[]{wrongIcon}).AllowsWrites,
            "wrong icon ID blocks");
        var wrongBg=Entry(op,game,true);wrongBg.ChangedBackground=true;
        wrongBg.AfterBackground="media/wrong";
        var oneBg=Stage(op,game,true,"H");oneBg.Media.Add("background","media/right");
        Check(!CatalogRecoveryConsistency.Inspect(new[]{oneBg},new[]{wrongBg}).AllowsWrites,
            "wrong background ID blocks");
        var untracked=Stage(op,game,true,"H");untracked.Media.Add("thumbnail","media/other");
        Check(!CatalogRecoveryConsistency.Inspect(new[]{untracked},new[]{Entry(op,game,true)}).AllowsWrites,
            "unknown staging slot blocks");
        Check(CatalogRecoveryConsistency.Inspect(new[]{correctStage},
            new[]{new EnrichmentEntry {OperationId=op,GameId=game,IgdbId=123,
                Committed=true,Undone=true,ChangedCover=true,AfterCover="media/cover-A",
                ChangedIcon=true,AfterIcon="media/icon-A",ChangedBackground=true,
                AfterBackground="media/background-A"}}).AllowsWrites,
            "undo retains historical artwork ownership");
        var legacyWithArtwork=Entry(op,game,true);
        legacyWithArtwork.ChangedCover=true;legacyWithArtwork.AfterCover="old/media/cover";
        Check(CatalogRecoveryConsistency.Inspect(new CatalogMediaStage[0],
            new[]{legacyWithArtwork}).AllowsWrites,"legacy field-only media remains supported");
        var falseAbandon=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"A")},
            new[]{Entry(op,game,true)});
        Check(!falseAbandon.AllowsWrites,"abandoned stage with journal owner blocks");
        var pending=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"H")},
            new[]{Entry(op,game,false)});
        Check(!pending.AllowsWrites && pending.PendingOperations==1,"uncommitted journal blocks");
        var pendingLegacy=CatalogRecoveryConsistency.Inspect(new CatalogMediaStage[0],
            new[]{Entry(op,game,false)});
        Check(!pendingLegacy.AllowsWrites,"legacy pending journal blocks");
        var duplicate=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"H"),Stage(op,game,true,"H")},
            new[]{Entry(op,game,true)});
        Check(!duplicate.AllowsWrites,"duplicate stages block");
        var unknown=CatalogRecoveryConsistency.Inspect(new[]{Stage(op,game,true,"X")},
            new EnrichmentEntry[0]);
        Check(!unknown.AllowsWrites,"unexpected stage outcome blocks");
        var onlyName=CatalogImportIntent.Describe(true,false,false,false,false,false,false,false);
        Check(onlyName=="Title","name preview");
        var fields=CatalogImportIntent.Describe(false,true,true,true,true,true,true,false);
        Check(fields=="Description, Cover, Icon","preview never promises absent screenshot");
        var none=CatalogImportIntent.Describe(false,true,false,false,false,false,false,false);
        Check(none.Contains("live check"),"unavailable fields described cautiously");
        // Exercise both actual persisted logs together, rather than testing
        // only synthetic in-memory records.
        var dir=Path.Combine(Path.GetTempPath(),"SMM-cross-journal-"+Guid.NewGuid().ToString("N"));
        try
        {
            var mediaLedger=new CatalogMediaStagingJournal(dir);
            var fieldJournal=new EnrichmentJournal(dir);
            var realOp=Guid.NewGuid();var realGame=Guid.NewGuid();
            mediaLedger.Begin(realOp,realGame);
            mediaLedger.Track(realOp,"cover","media/001");
            fieldJournal.Prepare(new EnrichmentEntry {OperationId=realOp,GameId=realGame,
                IgdbId=10,BeforeName="Old",AfterName="New",ChangedName=true,
                BeforeCover="",AfterCover="media/001",ChangedCover=true});
            mediaLedger.HandOff(realOp);
            fieldJournal.Commit(realOp);
            var reread=CatalogRecoveryConsistency.Inspect(
                new CatalogMediaStagingJournal(dir).AllStages(),
                new EnrichmentJournal(dir).AllEntries());
            Check(reread.AllowsWrites && reread.HandOffs==1,"persisted handoff/journal replay");
        }
        finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+checks+" cross-ledger recovery and import-preview checks.");
    }
}
