using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

// Opt-in synthetic performance benchmark. NEVER reads the Playnite library or
// the user's actual journal. Temporary fixture data is deleted on completion.
internal static class JournalReplayBenchmarkTests
{
    private static string Stamp(double milliseconds)
    {
        return milliseconds.ToString("F2",CultureInfo.InvariantCulture);
    }

    private static string Encoded(SmartMetadataMatcher.EnrichmentEntry entry)
    {
        var serializer=new DataContractJsonSerializer(typeof(SmartMetadataMatcher.EnrichmentEntry));
        using(var stream=new MemoryStream())
        {
            serializer.WriteObject(stream,entry);
            return Convert.ToBase64String(stream.ToArray());
        }
    }

    private static void Seed(string dir,int count)
    {
        var enrich=new StringBuilder();
        var media=new StringBuilder();
        var stamp=DateTime.UtcNow.Ticks;
        for(var i=0;i<count;i++)
        {
            var op=Guid.NewGuid();var game=Guid.NewGuid();
            var entry=new SmartMetadataMatcher.EnrichmentEntry {
                OperationId=op,GameId=game,IgdbId=10000+i,
                BeforeName="Before game "+i,AfterName="Game "+i,
                ChangedName=true,PreparedUtc=DateTime.UtcNow
            };
            var id=op.ToString("N");
            enrich.Append("P|").Append(Encoded(entry)).Append('\n');
            enrich.Append("C|").Append(id).Append('|').Append(stamp).Append('\n');
            if(i%5==0) enrich.Append("U|").Append(id).Append('|').Append(stamp).Append('\n');
            media.Append("B|").Append(id).Append('|').Append(game.ToString("N")).Append('\n');
            media.Append("M|").Append(id).Append("|cover|").Append(Convert.ToBase64String(
                Encoding.UTF8.GetBytes("synthetic-cover-"+i))).Append('\n');
            media.Append("H|").Append(id).Append('\n');
        }
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,"enrichment-journal-v2.log"),enrich.ToString(),new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(dir,"media-staging-v1.log"),media.ToString(),new UTF8Encoding(false));
    }

    private static double Timed(Action action)
    {
        var watch=Stopwatch.StartNew();
        action();watch.Stop();return watch.Elapsed.TotalMilliseconds;
    }

    private static double Median(double a,double b,double c)
    {
        return Math.Max(Math.Min(a,b),Math.Min(Math.Max(a,b),c));
    }

    private static void BenchmarkCount(int count,bool verbose)
    {
        var dir=Path.Combine(Path.GetTempPath(),"SMM-synthetic-benchmark-"+Guid.NewGuid().ToString("N"));
        try
        {
            Seed(dir,count);
            var journal=new SmartMetadataMatcher.EnrichmentJournal(dir);
            // Warm up the actual source-code parsers, then measure 3 passes.
            if(journal.AllEntries().Count!=count || journal.MediaStaging.AllStages().Count!=count)
                throw new Exception("Synthetic journal fixture mismatch");
            double j1=Timed(()=>CheckEntries(journal,count));
            double j2=Timed(()=>CheckEntries(journal,count));
            double j3=Timed(()=>CheckEntries(journal,count));
            double m1=Timed(()=>CheckStages(journal,count));
            double m2=Timed(()=>CheckStages(journal,count));
            double m3=Timed(()=>CheckStages(journal,count));

            // A bounded real journal-only transaction exercises repeated replay,
            // durable Begin/Track/Prepare/HandOff/Commit, but no Playnite APIs.
            // Includes fs.Flush(true) costs and is NOT a full metadata-import benchmark.
            var elapsed=Timed(()=>{
                CheckEntries(journal,count);CheckStages(journal,count);
                var op=Guid.NewGuid();var game=Guid.NewGuid();
                journal.MediaStaging.Begin(op,game);
                journal.MediaStaging.Track(op,"cover","synthetic-new-cover");
                journal.Prepare(new SmartMetadataMatcher.EnrichmentEntry {
                    OperationId=op,GameId=game,IgdbId=999999,
                    BeforeName="Before",AfterName="After",ChangedName=true
                });
                journal.MediaStaging.HandOff(op);
                journal.Commit(op);
            });
            if(journal.AllEntries().Count!=count+1 ||
               journal.MediaStaging.AllStages().Count!=count+1 ||
               journal.MediaStaging.Unresolved().Count!=0)
                throw new Exception("Synthetic journal transaction failed durability checks");
            if(verbose) Console.WriteLine("BENCH records="+count+
                " entry_replay_median_ms="+Stamp(Median(j1,j2,j3))+
                " stage_replay_median_ms="+Stamp(Median(m1,m2,m3))+
                " single_journal_transaction_ms="+Stamp(elapsed));
        }
        finally
        {
            // Only own random synthetic fixture directory is deleted.
            if(Directory.Exists(dir))Directory.Delete(dir,true);
        }
    }
    private static void CheckEntries(SmartMetadataMatcher.EnrichmentJournal journal,int expected)
    {
        var entries=journal.AllEntries();
        if(entries.Count!=expected || entries[0].GameId==Guid.Empty)
            throw new Exception("Invalid synthetic enrichment journal replay");
    }
    private static void CheckStages(SmartMetadataMatcher.EnrichmentJournal journal,int expected)
    {
        var stages=journal.MediaStaging.AllStages();
        if(stages.Count!=expected || !stages[0].Closed)
            throw new Exception("Invalid synthetic media staging journal replay");
    }
    internal static void Run()
    {
        BenchmarkCount(5,false);
        Console.WriteLine("PASS: synthetic journal replay fixture and write cycle checks. No Playnite data accessed.");
    }
    internal static void RunBenchmark(bool large)
    {
        Console.WriteLine("BENCH: Synthetic .NET Framework journal baseline; no Playnite data used.");
        Console.WriteLine("BENCH: Median of three warm replays, plus one journal-only write cycle. No performance pass/fail thresholds.");
        foreach(var count in large?new[]{300,3000,10000}:new[]{300,3000})BenchmarkCount(count,true);
        Console.WriteLine("PASS: synthetic journal benchmark completed; production code unchanged.");
    }
}
