using System;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class MetadataPlanTests
{
    private static int count;
    private static void Check(bool ok, string name)
    {
        count++;
        if (!ok) throw new Exception("Metadata test failed: " + name);
    }
    internal static void Run()
    {
        Check(MetadataPlan.HtmlFromSummary("A & B <script>x</script>").Contains("&lt;script&gt;"), "HTML encoded");
        Check(MetadataPlan.HtmlFromSummary("One\nTwo").Contains("One<br />Two"), "newlines encoded");
        Check(MetadataPlan.HtmlFromSummary(" ")=="", "empty description ignored");
        Check(MetadataPlan.Missing(null) && MetadataPlan.Missing("  ") && !MetadataPlan.Missing("<p>Added</p>"), "missing fields");
        Check(MetadataPlan.SafeImageId("co1a2_3-X") && !MetadataPlan.SafeImageId("../oops") &&
            !MetadataPlan.SafeImageId("https://example.org"), "image ID containment");
        var jpg=new byte[128]; jpg[0]=255; jpg[1]=216; jpg[126]=255; jpg[127]=217;
        Check(MetadataPlan.LooksLikeJpeg(jpg), "JPEG signature");
        jpg[127]=0;
        Check(!MetadataPlan.LooksLikeJpeg(jpg), "reject invalid JPEG");
        Check(MetadataPlan.MatchesExpected("Name","Name",null,"","",null), "null vs blank matches");
        Check(!MetadataPlan.MatchesExpected("A","B","","","",""), "stale name detected");
        var location=Path.Combine(Path.GetTempPath(),"SMM-metadata-journal-test-"+Guid.NewGuid().ToString("N"));
        try
        {
            var journal=new MetadataJournal(location);
            Check(journal.LatestPending()==null, "empty journal");
            var e=new MetadataEntry {
                OperationId=Guid.NewGuid(), GameId=Guid.NewGuid(),IgdbId=123456,GameName="Pokémon LeafGreen",
                BeforeDescription="", AfterDescription="<p>Game &amp; extras</p>",
                BeforeCover="", AfterCover="opaque-db-image-id",ChangedDescription=true,ChangedCover=true
            };
            journal.Prepare(e);
            var got=journal.LatestPending();
            Check(got!=null && got.GameName=="Pokémon LeafGreen" && got.AfterDescription==e.AfterDescription,
                "journal Unicode and HTML round-trip");
            Check(MetadataPlan.CanUndo(got,got.AfterDescription,got.AfterCover), "allow exact undo");
            Check(!MetadataPlan.CanUndo(got,"user edit",got.AfterCover), "protect modified description");
            Check(!MetadataPlan.CanUndo(got,got.AfterDescription,"user artwork"), "protect changed artwork");
            Check(MetadataPlan.IsBefore(got,"",""), "interrupted uncommitted record");
            journal.MarkUndone(e.OperationId);
            Check(journal.Pending().Count==0, "undo marker applied");
            var e2=new MetadataEntry {OperationId=Guid.NewGuid(),GameId=Guid.NewGuid(),IgdbId=2,
                GameName="Cover-only",BeforeDescription="existing description",AfterDescription="existing description",
                BeforeCover="",AfterCover="cover2",ChangedCover=true};
            journal.Prepare(e2);
            got=journal.LatestPending();
            Check(MetadataPlan.CanUndo(got,"new user description",got.AfterCover),
                "undo ignores unrelated description edit");
            var file=Path.Combine(location,"metadata-journal-v1.log");
            File.AppendAllText(file,"TRUNCATED\n");
            bool rejected=false;
            try { journal.Pending(); } catch (InvalidDataException) { rejected=true; }
            Check(rejected,"corrupted journal fails closed");
        }
        finally { if (Directory.Exists(location)) Directory.Delete(location,true); }
        Console.WriteLine("PASS: " + count + " metadata preflight, HTML, image, journal and undo checks.");
    }
}
