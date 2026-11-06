using System;
using System.IO;
using System.Linq;
using System.Text;
using SmartMetadataMatcher;

internal static class CatalogScanCacheTests
{
    private static int checks;
    private static void Check(bool value,string label)
    {
        checks++;
        if(!value)throw new Exception("FAIL: catalog scan cache: "+label);
    }
    internal static void Run()
    {
        var folder=Path.Combine(Path.GetTempPath(),"SMM-cache-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var cache=new CatalogScanCache(folder);
            cache.Load();
            var now=DateTime.UtcNow;
            var input=new CatalogInput{Id=Guid.NewGuid(),Name="05 Super Mario Adv",
                Platform="Nintendo Game Boy Advance",MissingCount=3};
            CatalogScanRecord found;
            Check(!cache.TryGet(input,now,out found),"empty cache miss");
            var entry=new CatalogScanRecord {GameId=input.Id,Name=input.Name,Platform=input.Platform,
                MissingCount=3,ScannedUtc=now.ToString("o",System.Globalization.CultureInfo.InvariantCulture),Candidate="Super Mario Advance",
                Status="Strong candidate",CatalogId="81197",Score="1.000",
                Evidence="correct platform",Summary="アドバンス title test",
                CoverId="co123",ScreenshotId="sc123",AvailableMetadata="cover icon",
                Queries="Super Mario Advance",FoundCount="8",ReleaseYear="2001"};
            cache.SaveBatch(new[]{entry},now);
            var reload=new CatalogScanCache(folder);reload.Load();
            Check(reload.TryGet(input,now,out found),"valid entry restored");
            Check(found.CatalogId=="81197" && found.Summary==entry.Summary,"Unicode round trip");
            Check(!reload.TryGet(new CatalogInput{Id=input.Id,Name="Super Mario Advance",
                Platform=input.Platform,MissingCount=3},now,out found),"rename invalidates");
            Check(!reload.TryGet(new CatalogInput{Id=input.Id,Name=input.Name,
                Platform="Nintendo Game Boy",MissingCount=3},now,out found),"platform invalidates");
            Check(!reload.TryGet(new CatalogInput{Id=input.Id,Name=input.Name,
                Platform=input.Platform,MissingCount=2},now,out found),"missing-field change invalidates");
            Check(!reload.TryGet(input,now.AddDays(8),out found),"expired cache invalidates");
            Check(!reload.TryGet(input,now.AddMinutes(-10),out found),"future cache invalidates");

            // v0.5.6: simulate a large scan with a process restart between
            // checkpoints. A new cache instance sees only fully saved chunks.
            var progressFolder=Path.Combine(folder,"progress");
            Directory.CreateDirectory(progressFolder);
            var progressCache=new CatalogScanCache(progressFolder);
            progressCache.Load();
            var checkpoint=new CatalogScanCheckpoint(progressCache);
            Check(CatalogScanCheckpoint.DefaultInterval==10,"bounded default checkpoint interval");
            var progressInputs=new System.Collections.Generic.List<CatalogInput>();
            for(int i=0;i<25;i++)
            {
                var item=new CatalogInput {Id=Guid.NewGuid(),Name="Scanned Game "+i,
                    Platform="Nintendo Game Boy Advance",MissingCount=1};
                progressInputs.Add(item);
                checkpoint.Add(new CatalogScanRecord {GameId=item.Id,Name=item.Name,
                    Platform=item.Platform,MissingCount=item.MissingCount,
                    ScannedUtc=now.ToString("o",System.Globalization.CultureInfo.InvariantCulture),
                    Status="Review",Candidate="Candidate "+i},now);
                if(i==8)
                {
                    Check(!File.Exists(Path.Combine(progressFolder,"catalog-scans-v1.json")),
                        "under ten results remain in memory until checkpoint");
                }
                if(i==9 || i==19)
                {
                    var restarted=new CatalogScanCache(progressFolder);restarted.Load();
                    CatalogScanRecord saved;
                    Check(restarted.TryGet(progressInputs[i],now,out saved),
                        "last result in completed checkpoint survives restart");
                    Check(checkpoint.SavedCount==i+1 && checkpoint.PendingCount==0,
                        "completed checkpoint clears only persisted buffer");
                }
            }
            Check(checkpoint.SavedCount==20 && checkpoint.PendingCount==5,
                "unsaved short tail retained for cancellation");
            var beforeFlush=new CatalogScanCache(progressFolder);beforeFlush.Load();
            CatalogScanRecord progressFound;
            Check(!beforeFlush.TryGet(progressInputs[24],now,out progressFound),
                "abrupt exit can lose only uncheckpointed short tail");
            checkpoint.Flush(now); // Mimics normal completion / cancellation.
            var afterFlush=new CatalogScanCache(progressFolder);afterFlush.Load();
            Check(afterFlush.TryGet(progressInputs[24],now,out progressFound),
                "cancellation/completion flush persists short tail");
            Check(checkpoint.SavedCount==25 && checkpoint.PendingCount==0,
                "final flush reports all persisted records");
            checkpoint.Flush(now);
            Check(checkpoint.SavedCount==25,"empty flush is idempotent");
            // Rejected malformed checkpoint cannot replace a prior valid snapshot.
            var invalidCheckpoint=new CatalogScanCheckpoint(afterFlush,1);
            bool failedCheckpoint=false;
            try {invalidCheckpoint.Add(new CatalogScanRecord(),now);}
            catch(InvalidDataException){failedCheckpoint=true;}
            Check(failedCheckpoint && invalidCheckpoint.PendingCount==1,
                "invalid checkpoint fails without dropping unsaved entries");
            var afterFailure=new CatalogScanCache(progressFolder);afterFailure.Load();
            Check(afterFailure.TryGet(progressInputs[24],now,out progressFound),
                "previously persisted results survive failed checkpoint");
            bool invalidInterval=false;
            try {new CatalogScanCheckpoint(afterFailure,0);}
            catch(ArgumentOutOfRangeException){invalidInterval=true;}
            Check(invalidInterval,"zero-length checkpoint prohibited");

            // Old v1 snapshots have no field mask, and remain read-only reusable
            // when the three original primary fields agree.
            var expanded=new CatalogInput{Id=input.Id,Name=input.Name,Platform=input.Platform,
                MissingMask=CatalogEnrichmentNeeds.AllMask,MissingCount=9};
            Check(reload.TryGet(expanded,now,out found),"legacy candidate survives secondary-field expansion");
            Check(!reload.TryGet(new CatalogInput{Id=input.Id,Name=input.Name,
                Platform=input.Platform,MissingMask=8,MissingCount=1},now,out found),
                "legacy candidate invalidated on primary change");
            var updated=new CatalogScanRecord{GameId=entry.GameId,Name=entry.Name,Platform=entry.Platform,
                MissingMask=CatalogEnrichmentNeeds.AllMask,MissingCount=9,
                ScannedUtc=entry.ScannedUtc,Status=entry.Status,Candidate=entry.Candidate};
            reload.SaveBatch(new[]{updated},now);
            var latest=new CatalogScanCache(folder);latest.Load();
            Check(latest.TryGet(expanded,now,out found),"new missing-field mask roundtrip");
            Check(!latest.TryGet(new CatalogInput{Id=input.Id,Name=input.Name,
                Platform=input.Platform,MissingMask=CatalogEnrichmentNeeds.AllMask^8,
                MissingCount=8},now,out found),"icon-only missingness change invalidates new cache");
            // Continue older assertions against legacy snapshot below, not the updated one.
            var archive=reload.ArchiveAndClear(now);
            Check(File.Exists(archive),"fresh scan archives old data");
            Check(!reload.TryGet(input,now,out found),"fresh scan invalidates memory");
            // No partial overwrite when malformed entries are submitted.
            bool rejected=false;
            try {reload.SaveBatch(new[]{new CatalogScanRecord()},now);}
            catch(InvalidDataException){rejected=true;}
            Check(rejected,"invalid data rejected");
            var corrupt=Path.Combine(folder,"catalog-scans-v1.json");
            File.WriteAllText(corrupt,"{not valid JSON",Encoding.UTF8);
            var original=File.ReadAllBytes(corrupt);
            rejected=false;
            try {new CatalogScanCache(folder).Load();}
            catch(Exception){rejected=true;}
            Check(rejected && original.SequenceEqual(File.ReadAllBytes(corrupt)),"corruption preserved");
            Check(!String.Join(" ",Directory.GetFiles(folder)).Contains("secret"),"no credential artifacts");
        }
        finally {Directory.Delete(folder,true);}
        Console.WriteLine("PASS: "+checks+" read-only scan resume/cache safety checks.");
    }
}
