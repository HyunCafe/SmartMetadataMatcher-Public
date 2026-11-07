using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartMetadataMatcher;

internal static class CatalogIgnoredTests
{
    internal static void Run()
    {
        var dir=Path.Combine(Path.GetTempPath(),"SMM-Ignored-Tests-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new CatalogIgnored(dir);
            if(store.Load().Count!=0)throw new Exception("Fresh ignored list must be empty");
            var a=Guid.NewGuid();var b=Guid.NewGuid();
            store.Save(new[]{a,b,a});
            var got=store.Load();
            if(got.Count!=2 || !got.Contains(a) || !got.Contains(b))
                throw new Exception("Ignored IDs did not persist exactly");
            store.Save(new[]{b});
            if(store.Load().Count!=1 || !store.Load().Contains(b))
                throw new Exception("Ignored restore did not persist");
            File.WriteAllText(Path.Combine(dir,"catalog-ignored-ids.txt"),"bad data");
            bool refused=false;
            try {store.Load();}catch(InvalidDataException){refused=true;}
            if(!refused)throw new Exception("Malformed ignored file must fail closed");
            Console.WriteLine("PASS: ignored catalog ID persistence, deduplication, restore and malformed-file protection.");
        }
        finally
        {
            if(Directory.Exists(dir))Directory.Delete(dir,true);
        }
    }
}
