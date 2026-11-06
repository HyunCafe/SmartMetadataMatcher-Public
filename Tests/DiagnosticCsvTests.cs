using System;
using System.Linq;
using SmartMetadataMatcher;

internal static class DiagnosticCsvTests
{
    internal static void Run()
    {
        var id = Guid.NewGuid();
        var rows = new[] {
            new DiagnosticRecord { Id = id, Name = "01 Pokemon Fire Red", Platform = "GBA", Source = "Emulator",
                MatcherStatus = "Ready", ProposedName = "Pokemon Fire Red", MatcherReason = "Two-digit prefix: 01" },
            new DiagnosticRecord { Id = Guid.NewGuid(), Name = "=1+1, \"test\"", HasCover = true,
                HasBackground = true, HasDescription = true, MatcherStatus = "No suggestion" }
        };
        if (!rows[0].NeedsCoreMetadata || rows[0].MissingCoreFields != "cover, background, description")
            throw new Exception("Missing-all detection failed");
        if (rows[1].NeedsCoreMetadata || rows[1].BackgroundOnlyMissing)
            throw new Exception("Core-complete detection failed");
        var bg = new DiagnosticRecord { HasCover = true, HasDescription = true, HasBackground = false };
        if (!bg.NeedsCoreMetadata || !bg.BackgroundOnlyMissing || bg.MissingCoreFields != "background")
            throw new Exception("Background-only detection failed");
        var csv = DiagnosticCsv.Serialize(rows);
        if (!csv.Contains("Missing all core fields") || !csv.Contains("All core fields present"))
            throw new Exception("CSV diagnostics: core metadata flags incorrect");
        if (!csv.Contains("'\u003d1+1, \"\"test\"\""))
            throw new Exception("CSV diagnostics: formula sanitization or quoting incorrect");
        if (!csv.Contains("01 Pokemon Fire Red") || !csv.Contains("No suggestion"))
            throw new Exception("CSV diagnostics: full-library and no-proposal labels missing");
        if (csv.Split('\n').Length != 4) // 1 header + 2 entries + trailing newline
            throw new Exception("CSV diagnostics: incorrect record count");
        Console.WriteLine("PASS: CSV serialization and 3 metadata classification checks. No Playnite database accessed.");
    }
}
