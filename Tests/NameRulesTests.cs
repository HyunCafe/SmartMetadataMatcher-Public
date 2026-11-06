using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class Program
{
    private static int checks;
    private static GameDescriptor G(string name, string platform = "GBA", string source = "Emulator")
    {
        return new GameDescriptor { Id = Guid.NewGuid(), Name = name, Platform = platform, Source = source };
    }
    private static void Expect(bool yes, string label)
    {
        checks++;
        if (!yes) throw new Exception("FAIL: " + label);
    }
    private static void ExpectProposal(IEnumerable<RenameProposal> p, string original,
        string wanted, string wantedStatus)
    {
        var row = p.Single(x => x.Original == original);
        Expect(row.Proposed == wanted, original + " -> " + wanted + " (got: " + row.Proposed + ")");
        Expect(row.Status == wantedStatus, original + " status: " + wantedStatus + " (got " + row.Status + ")");
    }
    private static void Main()
    {
        var roms = new List<GameDescriptor>
        {
            G("01 Pokemon Fire Red"), G("02 Pokemon Leaf Green"), G("03 Crash Bandicoot 2"),
            G("14 Sonic Advance"), G("25 Golden Sun"), G("Mega Man Xtreme 2"),
            G("007 Nightfire"), G("Legend of Zelda, The - Oracle of Ages"),
            G("Pokemon Diamond", "GBC"), G("20 Minutes Till Dawn", "PC", "Local")
        };
        var proposals = NameRules.Analyze(roms);
        ExpectProposal(proposals, "01 Pokemon Fire Red", "Pokemon Fire Red", "Ready");
        ExpectProposal(proposals, "02 Pokemon Leaf Green", "Pokemon Leaf Green", "Ready");
        ExpectProposal(proposals, "03 Crash Bandicoot 2", "Crash Bandicoot 2", "Ready");
        ExpectProposal(proposals, "14 Sonic Advance", "Sonic Advance", "Ready");
        ExpectProposal(proposals, "25 Golden Sun", "Golden Sun", "Ready");
        ExpectProposal(proposals, "Legend of Zelda, The - Oracle of Ages",
            "The Legend of Zelda - Oracle of Ages", "Review");
        ExpectProposal(proposals, "20 Minutes Till Dawn", "Minutes Till Dawn", "Review");
        Expect(!proposals.Any(x => x.Original == "007 Nightfire"), "007 not stripped");
        Expect(!proposals.Any(x => x.Original == "Mega Man Xtreme 2"), "sequel number preserved");
        Expect(!proposals.Any(x => x.Original == "Pokemon Diamond"), "unchanged ambiguous title");
        var collision = NameRules.Analyze(new[] {
            G("01 Golden Sun"), G("02 Golden Sun"), G("03 Sonic Advance")
        });
        Expect(collision.Count(x => x.Proposed == "Golden Sun" && x.Status == "Review") == 2,
            "same-platform collision blocks Ready status");
        var two = NameRules.Analyze(new[] { G("01 Golden Sun"), G("02 Pokemon Leaf Green") });
        Expect(two.All(x => x.Status == "Review"), "two numbers are not enough to infer a collection");
        var psp = NameRules.Analyze(new[] {
            G("Class Of Heroes PSP", "Sony PlayStation Portable"),
            G("Cladun X 2 PSP", "Sony PlayStation Portable"),
            G("Final Fantasy VII Crisis Core", "Sony PlayStation Portable"),
            G("Mega Man Xtreme 2 PSP", "Nintendo Game Boy Advance"),
            G("Phantasy Star Portable PSP", "Sony PlayStation Portable")
        });
        ExpectProposal(psp, "Class Of Heroes PSP", "Class Of Heroes", "Review");
        ExpectProposal(psp, "Cladun X 2 PSP", "ClaDun x2", "Review");
        ExpectProposal(psp, "Final Fantasy VII Crisis Core", "Crisis Core: Final Fantasy VII", "Review");
        ExpectProposal(psp, "Phantasy Star Portable PSP", "Phantasy Star Portable", "Review");
        Expect(!psp.Any(x => x.Original == "Mega Man Xtreme 2 PSP"),
            "PSP suffix retained when actual platform is not PSP");
        var pspConflict = NameRules.Analyze(new[] {
            G("Class Of Heroes PSP", "Sony PlayStation Portable"),
            G("Class Of Heroes", "Sony PlayStation Portable")
        });
        ExpectProposal(pspConflict, "Class Of Heroes PSP", "Class Of Heroes", "Review");
        Expect(pspConflict.Single().Reason.Contains("collision"), "PSP collision flagged");
        CatalogCoreTests.Run();
        CatalogReviewActionsTests.Run();
        CatalogIgnoredTests.Run();
        CatalogScanCacheTests.Run();
        CatalogConfirmedIdentitiesTests.Run();
        CatalogMediaStagingJournalTests.Run();
        CatalogRecoveryConsistencyTests.Run();
        MetadataPlanTests.Run();
        EnrichmentPlanTests.Run();
        EnrichmentJobPlanTests.Run();
        CatalogBatchProgressTests.Run();
        DiagnosticCsvTests.Run();
        RenameJournalTests.Run();
        Console.WriteLine("PASS: " + checks + " focused checks. No Playnite database accessed.");
    }
}
