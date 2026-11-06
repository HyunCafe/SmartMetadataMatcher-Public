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
