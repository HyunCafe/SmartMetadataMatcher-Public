using System;
using System.IO;
using System.Linq;
using System.Text;
using SmartMetadataMatcher;

// Synthetic durable-event interruption matrix. All logs live in fresh TEMP folders.
// It does NOT crash Playnite, simulate AddFile itself, or modify a real library.
internal static class JournalFaultMatrixTests
{
    private static int checks;
    private static void Check(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception("Recovery fault matrix FAILED: " + description);
    }
    private static string PathFor(string dir, bool media)
    {
        return Path.Combine(dir, media ? "media-staging-v1.log" : "enrichment-journal-v2.log");
    }
    private static byte[] Bytes(string file)
    {
        return File.Exists(file) ? File.ReadAllBytes(file) : null;
    }
    private static bool Same(byte[] a, byte[] b)
    {
        if (a == null || b == null) return a == null && b == null;
        return a.SequenceEqual(b);
    }
    private static CatalogRecoveryReport Inspect(string dir)
    {
        // New readers model reopening the extension after an abrupt interruption.
        return CatalogRecoveryConsistency.Inspect(
            new CatalogMediaStagingJournal(dir).AllStages(),
            new EnrichmentJournal(dir).AllEntries());
    }
    private static void InspectCleanly(string dir, bool allowed, string state)
    {
        var mediaBefore = Bytes(PathFor(dir,true));
        var journalBefore = Bytes(PathFor(dir,false));
        var first = Inspect(dir);
        var second = Inspect(dir);
        Check(first.AllowsWrites == allowed && second.AllowsWrites == allowed,
            state + " consistent after restart");
        Check(Same(mediaBefore, Bytes(PathFor(dir,true))) &&
              Same(journalBefore, Bytes(PathFor(dir,false))),
            state + " read-only recovery never rewrites either journal");
    }
    private static EnrichmentEntry Entry(Guid op, Guid game, string mediaId)
    {
        var e = new EnrichmentEntry { OperationId=op, GameId=game, IgdbId=157,
            ChangedName=true, BeforeName="Original title", AfterName="Verified title" };
        if (mediaId != null)
        {
            e.ChangedCover=true;
            e.BeforeCover="";
            e.AfterCover=mediaId;
        }
        return e;
    }
    private static void RejectRead(string dir, bool media, string why)
    {
        var path=PathFor(dir,media);
        var before=Bytes(path);
        bool rejected=false;
        try { Inspect(dir); }
        catch (InvalidDataException) { rejected=true; }
        Check(rejected,why+" fails closed");
        Check(Same(before, Bytes(path)),why+" does not rewrite corrupt records");
    }
    private static void Scenario(string title, Action<string> action)
    {
        var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "SMM-fault-matrix-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { action(dir); }
        catch (Exception ex) { throw new Exception("Scenario: " + title,ex); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
    internal static void Run()
    {
        checks=0;
        Scenario("clean start", dir => InspectCleanly(dir,true,"empty installation"));
        Scenario("Begin before AddFile", dir => {
            var ledger=new CatalogMediaStagingJournal(dir);
            var op=Guid.NewGuid();ledger.Begin(op,Guid.NewGuid());
            InspectCleanly(dir,false,"open Begin with unknown AddFile result");
            Check(Inspect(dir).OpenStages==1,"unresolved stage is reported");
            bool refused=false;
            var before=Bytes(PathFor(dir,true));
            try { ledger.Begin(Guid.NewGuid(),Guid.NewGuid()); }
            catch (InvalidOperationException) { refused=true; }
            Check(refused && Same(before,Bytes(PathFor(dir,true))),
                "unresolved artwork blocks another game without a new event");
        });
        Scenario("after returned artwork id", dir => {
            var ledger=new CatalogMediaStagingJournal(dir);
            var op=Guid.NewGuid();ledger.Begin(op,Guid.NewGuid());
            ledger.Track(op,"cover","playnite/cover-1");
            InspectCleanly(dir,false,"tracked media without metadata prepare");
            Check(ledger.Unresolved().Single().Media["cover"]=="playnite/cover-1",
                "returned media reference survives restart");
        });
        Scenario("after metadata Prepare before Handoff", dir => {
            var op=Guid.NewGuid();var game=Guid.NewGuid();
            var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,game);ledger.Track(op,"cover","playnite/cover-1");
            new EnrichmentJournal(dir).Prepare(Entry(op,game,"playnite/cover-1"));
            InspectCleanly(dir,false,"prepared but unresolved media stage");
            Check(Inspect(dir).PendingOperations==1,"uncommitted prepare is visible");
        });
        Scenario("after Handoff before Commit", dir => {
            var op=Guid.NewGuid();var game=Guid.NewGuid();
            var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,game);ledger.Track(op,"cover","playnite/cover-1");
            var journal=new EnrichmentJournal(dir);
            journal.Prepare(Entry(op,game,"playnite/cover-1"));ledger.HandOff(op);
            InspectCleanly(dir,false,"handoff but uncommitted metadata");
            Check(Inspect(dir).OpenStages==0 && Inspect(dir).PendingOperations==1,
                "closed handoff does not imply a committed database write");
        });
        Scenario("after Commit", dir => {
            var op=Guid.NewGuid();var game=Guid.NewGuid();
            var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,game);ledger.Track(op,"cover","playnite/cover-1");
            var journal=new EnrichmentJournal(dir);
            journal.Prepare(Entry(op,game,"playnite/cover-1"));ledger.HandOff(op);
            journal.Commit(op);
            InspectCleanly(dir,true,"fully committed handoff");
            Check(journal.AllEntries().Single().Committed,"durable commit marker survives restart");
            journal.MarkUndone(op);
            InspectCleanly(dir,true,"fully recorded undo");
            Check(journal.AllEntries().Single().Undone,"undone history is retained");
        });
        Scenario("clean no-change skip", dir => {
            var ledger=new CatalogMediaStagingJournal(dir);
            var op=Guid.NewGuid();ledger.Begin(op,Guid.NewGuid());
            ledger.AbandonClean(op);
            InspectCleanly(dir,true,"zero-artwork clean abandon");
            Check(new EnrichmentJournal(dir).AllEntries().Count==0,
                "clean skip has no metadata undo record");
        });
        Scenario("legacy metadata-only pending", dir => {
            var op=Guid.NewGuid();var journal=new EnrichmentJournal(dir);
            journal.Prepare(Entry(op,Guid.NewGuid(),null));
            InspectCleanly(dir,false,"legacy metadata-only unfinished prepare");
            journal.Commit(op);
            InspectCleanly(dir,true,"legacy metadata-only commit");
            journal.MarkUndone(op);
            InspectCleanly(dir,true,"legacy metadata-only undo");
        });
        Scenario("orphan media handoff", dir => {
            var ledger=new CatalogMediaStagingJournal(dir);
            var op=Guid.NewGuid();ledger.Begin(op,Guid.NewGuid());ledger.HandOff(op);
            InspectCleanly(dir,false,"handoff without metadata owner");
            Check(Inspect(dir).Problems.Any(x=>x.Contains("Orphan")),
                "orphan is clearly diagnosed");
        });
        Scenario("cross-ledger owner mismatch", dir => {
            var op=Guid.NewGuid();var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,Guid.NewGuid());ledger.Track(op,"cover","media/1");
            var journal=new EnrichmentJournal(dir);
            journal.Prepare(Entry(op,Guid.NewGuid(),"media/1"));
            ledger.HandOff(op);journal.Commit(op);
            InspectCleanly(dir,false,"different game IDs for same operation");
        });
        Scenario("cross-ledger artwork mismatch", dir => {
            var op=Guid.NewGuid();var game=Guid.NewGuid();
            var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,game);ledger.Track(op,"cover","media/original");
            var journal=new EnrichmentJournal(dir);
            journal.Prepare(Entry(op,game,"media/different"));
            ledger.HandOff(op);journal.Commit(op);
            InspectCleanly(dir,false,"media ID differs from journal owner");
        });
        Scenario("truncated field journal", dir => {
            var op=Guid.NewGuid();var journal=new EnrichmentJournal(dir);
            journal.Prepare(Entry(op,Guid.NewGuid(),null));
            File.AppendAllText(PathFor(dir,false),"C|TRUNCATED",new UTF8Encoding(false));
            RejectRead(dir,false,"partial commit marker");
        });
        Scenario("truncated media journal", dir => {
            var op=Guid.NewGuid();var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,Guid.NewGuid());
            File.AppendAllText(PathFor(dir,true),"M|"+op.ToString("N")+"|cover|bad-base64!",new UTF8Encoding(false));
            RejectRead(dir,true,"partial artwork record");
        });
        Scenario("unexpected commit marker", dir => {
            File.WriteAllText(PathFor(dir,false),"C|"+Guid.NewGuid().ToString("N")+"\n",new UTF8Encoding(false));
            RejectRead(dir,false,"commit without prepare");
        });
        Scenario("out-of-sequence artwork", dir => {
            var op=Guid.NewGuid();var ledger=new CatalogMediaStagingJournal(dir);
            ledger.Begin(op,Guid.NewGuid());
            ledger.AbandonClean(op);
            File.AppendAllText(PathFor(dir,true),"H|"+op.ToString("N")+"\n",new UTF8Encoding(false));
            RejectRead(dir,true,"handoff after clean abandon");
        });
        Console.WriteLine("PASS: " + checks +
            " synthetic cross-ledger interruption/fail-closed assertions; disposable files only, no Playnite access.");
    }
}
