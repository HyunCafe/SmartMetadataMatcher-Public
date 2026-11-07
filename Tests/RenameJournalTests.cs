using System;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class RenameJournalTests
{
    internal static void Run()
    {
        var location = Path.Combine(Path.GetTempPath(), "SMM-journal-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var journal = new RenameJournal(location);
            var game = Guid.NewGuid();
            var game2 = Guid.NewGuid();
            var batch1 = Guid.NewGuid();
            var batch2 = Guid.NewGuid();
            if (journal.Pending().Count != 0) throw new Exception("Expected empty journal");
            journal.Prepare(batch1, game, "01 Pokémon | Fire Red", "Pokémon FireRed");
            journal.Prepare(batch1, game2, "14 Sonic Advance", "Sonic Advance");
            var pending = journal.Pending();
            if (pending.Count != 2 || pending.First(e => e.GameId == game).Before != "01 Pokémon | Fire Red")
                throw new Exception("UTF-8 title round-trip or pending count failed");
            journal.Prepare(batch2, game, "Pokémon FireRed", "Pokémon FireRed DX");
            pending = journal.Pending();
            if (pending.Count != 2 || pending.First(e => e.GameId == game).After != "Pokémon FireRed DX")
                throw new Exception("Newest active game change must take priority");
            if (journal.LatestPendingBatch().Count != 1 || journal.LatestPendingBatch()[0].BatchId != batch2)
                throw new Exception("Latest batch selection failed");
            journal.MarkUndone(batch2, game);
            if (journal.Pending().First(e => e.GameId == game).After != "Pokémon FireRed")
                throw new Exception("Undo should expose previous active record");
            journal.MarkUndone(batch1, game);
            journal.MarkUndone(batch1, game2);
            if (journal.Pending().Count != 0) throw new Exception("All entries must be undone");
            Console.WriteLine("PASS: journal round trip, batch selection, stacked undo, and UTF-8 titles.");
        }
        finally { if (Directory.Exists(location)) Directory.Delete(location, true); }
    }
}
