using System;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogConfirmedIdentitiesTests
{
    private static int count;
    private static void Check(bool condition,string name)
    {
        count++;if(!condition)throw new Exception("Confirmed identity test failed: "+name);
    }
    internal static void Run()
    {
        var folder=Path.Combine(Path.GetTempPath(),"SMM-confirm-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var store=new CatalogConfirmedIdentities(folder);
            var id=Guid.NewGuid();
            Check(!store.Has(id),"starts empty");
            Check(!store.TryMatch(id,"05 Super Mario Adv","Nintendo Game Boy Advance",81197,"Super Mario Advance"),"absent not trusted");
            store.Confirm(id,"05 Super Mario Adv","Nintendo Game Boy Advance",81197,"Super Mario Advance",DateTime.UtcNow);
            var loaded=new CatalogConfirmedIdentities(folder);loaded.Load();
            Check(loaded.Has(id),"persisted");
            Check(loaded.TryMatch(id,"05 Super Mario Adv","Nintendo Game Boy Advance",81197,"Super Mario Advance"),"exact identity reused");
            Check(!loaded.TryMatch(id,"Super Mario Advance (GBA)","Nintendo Game Boy Advance",81197,"Super Mario Advance"),"changed name invalidates");
            Check(!loaded.TryMatch(id,"05 Super Mario Adv","Nintendo Game Boy",81197,"Super Mario Advance"),"changed platform invalidates");
            Check(!loaded.TryMatch(id,"05 Super Mario Adv","Nintendo Game Boy Advance",81198,"Super Mario Advance"),"new ID invalidates");
            Check(!loaded.TryMatch(id,"05 Super Mario Adv","Nintendo Game Boy Advance",81197,"Other edition"),"different edition invalidates");
            bool failed=false;
            try {loaded.Confirm(Guid.Empty,"x","GBA",1,"Game",DateTime.UtcNow);}catch(InvalidDataException){failed=true;}
            Check(failed,"invalid ID rejected");
            var bulk=System.Linq.Enumerable.Range(0,46).Select(i=>new ConfirmedIdentity {
                GameId=Guid.NewGuid(),GameName="Reviewed "+i,Platform="PC (Windows)",
                IgdbId=100+i,CatalogTitle="IGDB "+i,
                ConfirmedUtc=DateTime.UtcNow.ToString("o",System.Globalization.CultureInfo.InvariantCulture)
            }).ToList();
            loaded.ConfirmMany(bulk);loaded.Load();
            Check(bulk.All(x=>loaded.TryMatch(x.GameId,x.GameName,x.Platform,x.IgdbId,x.CatalogTitle)),
                "46 identity decisions persisted atomically");
            var existingBytes=File.ReadAllBytes(Path.Combine(folder,"confirmed-igdb-identities-v1.json"));
            var bad=bulk.Select(x=>x).ToList();bad.Add(bulk[0]);
            bool invalidBatch=false;
            try {loaded.ConfirmMany(bad);}catch(InvalidDataException){invalidBatch=true;}
            Check(invalidBatch,"duplicate batch identity rejected");
            Check(existingBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(folder,"confirmed-igdb-identities-v1.json"))),
                "failed batch preserves exact identity file bytes");
            loaded.Forget(id);loaded.Load();Check(!loaded.Has(id),"forget persists");
            var path=Path.Combine(folder,"confirmed-igdb-identities-v1.json");
            File.WriteAllText(path,"{ corrupted");
            failed=false;
            try {loaded.Load();}catch(InvalidDataException){failed=true;}
            Check(failed,"corrupt data rejected");
            Check(File.ReadAllText(path)=="{ corrupted","corrupt data preserved");
            Console.WriteLine("PASS: "+count+" user-confirmed identity persistence and invalidation checks.");
        }
        finally {Directory.Delete(folder,true);}
    }
}
