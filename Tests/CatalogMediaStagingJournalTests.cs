using System;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogMediaStagingJournalTests
{
    private static int count;
    private static void Check(bool condition,string message)
    {
        if(!condition)throw new Exception("Media staging safety test FAILED: "+message);
        count++;
    }
    internal static void Run()
    {
        var dir=Path.Combine(Path.GetTempPath(),"SMM-media-ledger-"+Guid.NewGuid().ToString("N"));
        try
        {
            var j=new CatalogMediaStagingJournal(dir);
            Check(j.Unresolved().Count==0,"fresh ledger is clear");
            var op=Guid.NewGuid();var game=Guid.NewGuid();j.Begin(op,game);
            Check(j.Unresolved().Single().GameId==game,"begin survives replay");
            bool blocked=false;
            try{j.Begin(Guid.NewGuid(),Guid.NewGuid());}catch(InvalidOperationException){blocked=true;}
            Check(blocked,"a pending stage blocks a second import");
            j.Track(op,"cover","images/éö/media-001");
            j.Track(op,"icon","images/icon-001");
            Check(new CatalogMediaStagingJournal(dir).Unresolved().Single().Media["cover"]=="images/éö/media-001",
                "tracked Unicode media ID survives restart");
            bool duplicateRejected=false;
            try{j.Track(op,"cover","another");}catch(InvalidOperationException){duplicateRejected=true;}
            Check(duplicateRejected,"duplicate media slot rejected");
            j.HandOff(op);
            Check(j.Unresolved().Count==0,"handoff closes stage");
            bool duplicateClose=false;
            try{j.HandOff(op);}catch(InvalidOperationException){duplicateClose=true;}
            Check(duplicateClose,"double close rejected");
            var op2=Guid.NewGuid();j.Begin(op2,Guid.NewGuid());j.Track(op2,"background","file2");
            j.AbandonClean(op2);
            Check(j.Unresolved().Count==0,"verified cleanup closes stage");
            // A no-change import closes a stage WITHOUT AddFile or field journaling.
            // The next game's Begin must still be possible.
            var cleanNoChange=Guid.NewGuid();j.Begin(cleanNoChange,Guid.NewGuid());
            j.AbandonClean(cleanNoChange);
            Check(j.AllStages().Single(x=>x.OperationId==cleanNoChange).Outcome=="A",
                "empty no-change stage durably closed");
            var afterSkip=Guid.NewGuid();j.Begin(afterSkip,Guid.NewGuid());
            j.AbandonClean(afterSkip);
            Check(j.Unresolved().Count==0,"next game may begin after clean no-change skip");
            var interrupted=Guid.NewGuid();j.Begin(interrupted,Guid.NewGuid());
            Check(new CatalogMediaStagingJournal(dir).Unresolved().Single().OperationId==interrupted,
                "interruption before AddFile result survives restart");
            j.Track(interrupted,"icon","playnite-id");
            Check(new CatalogMediaStagingJournal(dir).Unresolved().Single().Media["icon"]=="playnite-id",
                "interruption after AddFile ID logging survives restart");
            var file=Path.Combine(dir,"media-staging-v1.log");
            File.AppendAllText(file,"CORRUPT|EVENT\n");
            bool corruptRejected=false;
            try{j.Unresolved();}catch(InvalidDataException){corruptRejected=true;}
            Check(corruptRejected,"corrupted ledger fails closed without truncation");
        }
        finally { if(Directory.Exists(dir))Directory.Delete(dir,true); }
        Console.WriteLine("PASS: "+count+" durable media staging and interruption-recovery checks.");
    }
}
