using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using SmartMetadataMatcher;

internal static class EnrichmentJournalReplayTests
{
    private static int checks;
    private static void Check(bool valid,string reason)
    {
        if(!valid) throw new Exception("Indexed replay regression: "+reason);
        checks++;
    }
    private static string Encode(EnrichmentEntry e)
    {
        var serializer=new DataContractJsonSerializer(typeof(EnrichmentEntry));
        using(var stream=new MemoryStream())
        {
            serializer.WriteObject(stream,e);
            return Convert.ToBase64String(stream.ToArray());
        }
    }
    private static void MustReject(string dir,string records,string reason)
    {
        var log=Path.Combine(dir,"enrichment-journal-v2.log");
        File.WriteAllText(log,records,new UTF8Encoding(false));
        bool rejected=false;
        try {new EnrichmentJournal(dir).AllEntries();}
        catch(InvalidDataException) {rejected=true;}
        Check(rejected,reason);
    }
    internal static void Run()
    {
        var dir=Path.Combine(Path.GetTempPath(),"SMM-indexed-replay-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var log=Path.Combine(dir,"enrichment-journal-v2.log");
            var source=new List<EnrichmentEntry>();
            var records=new StringBuilder();
            var tick=DateTime.UtcNow.Ticks;
            for(int i=0;i<400;i++)
            {
                var e=new EnrichmentEntry{OperationId=Guid.NewGuid(),GameId=Guid.NewGuid(),
                    IgdbId=2000+i,ChangedName=true,BeforeName="Old "+i,AfterName="New "+i};
                source.Add(e);
                records.Append("P|").Append(Encode(e)).Append('\n');
                records.Append("C|").Append(e.OperationId.ToString("N"));
                if(i%2==0)records.Append('|').Append(tick);
                records.Append('\n');
                if(i%4==0)
                {
                    records.Append("U|").Append(e.OperationId.ToString("N"));
                    if(i%2==0)records.Append('|').Append(tick);
                    records.Append('\n');
                }
            }
            File.WriteAllText(log,records.ToString(),new UTF8Encoding(false));
            var j=new EnrichmentJournal(dir);
            var all=j.AllEntries();
            Check(all.Count==400,"all history records retained");
            Check(all[0].OperationId==source[399].OperationId &&
                  all[399].OperationId==source[0].OperationId,"newest-first order preserved");
            Check(all.All(e=>e.Committed),"commit flags preserved");
            Check(all.Count(e=>e.Undone)==100,"undo flags preserved");
            Check(j.OpenEntries().Count==300 && j.Committed().Count==300,
                "active committed entries skip undone history");
            Check(all.Single(e=>e.OperationId==source[0].OperationId).UndoneUtc.Ticks==tick,
                "dated undo marker retained");
            Check(all.Single(e=>e.OperationId==source[1].OperationId).CommittedUtc==default(DateTime),
                "legacy undated marker stays unknown");
            // Disk state must be reread on every invocation, not served from a stale cache.
            var fresh=new EnrichmentEntry{OperationId=Guid.NewGuid(),GameId=Guid.NewGuid(),
                IgdbId=9999,BeforeName="Before",AfterName="After",ChangedName=true};
            File.AppendAllText(log,"P|"+Encode(fresh)+"\n",new UTF8Encoding(false));
            Check(j.AllEntries()[0].OperationId==fresh.OperationId && j.AllEntries().Count==401,
                "external appended operation observed on next replay");
            var valid="P|"+Encode(source[0])+"\n";
            var op=source[0].OperationId.ToString("N");
            MustReject(dir,valid+valid,"duplicate prepare blocked");
            MustReject(dir,valid+"C|"+op+"\nC|"+op+"\n","double commit blocked");
            MustReject(dir,valid+"U|"+op+"\nU|"+op+"\n","double undo blocked");
            MustReject(dir,valid+"U|"+op+"\nC|"+op+"\n","commit after undo blocked");
            MustReject(dir,"C|"+Guid.NewGuid().ToString("N")+"\n","unknown marker blocked");
            MustReject(dir,valid+"C|"+op+"|not-ticks\n","malformed timestamp blocked");
            MustReject(dir,valid+"U|"+op+"|9999999999999999999\n","overflow timestamp blocked");
            MustReject(dir,"P|invalid-base64\n","corrupt prepare blocked");
            MustReject(dir,valid+"bad-record\n","truncated record blocked");
            // Regression tests use only disposable synthetic fixtures.
            Console.WriteLine("PASS: "+checks+" indexed journal replay/ordering/fail-closed checks; no Playnite data accessed.");
        }
        finally
        {
            if(Directory.Exists(dir))Directory.Delete(dir,true);
        }
    }
}
