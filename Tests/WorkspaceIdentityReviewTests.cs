using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartMetadataMatcher
{
    internal static class WorkspaceIdentityReviewTests
    {
        private static int n;
        private static void Check(bool yes, string label)
        {
            n++;if (!yes) throw new Exception("Native identity review: " + label);
        }
        internal static void Run()
        {
            Check(!WorkspaceIdentityRules.ValidSearch(null), "null query");
            Check(!WorkspaceIdentityRules.ValidSearch(" "), "blank query");
            Check(!WorkspaceIdentityRules.ValidSearch("a"), "short query");
            Check(!WorkspaceIdentityRules.ValidSearch(new string('x',121)), "long query");
            Check(!WorkspaceIdentityRules.ValidSearch("game\nother"), "control-character query");
            Check(WorkspaceIdentityRules.ValidSearch("Another Game"), "search title allowed");
            var id=Guid.NewGuid();
            var input = new CatalogInput {Id=id,Name="Sample Game",Platform="Game Boy Advance",
                MissingMask=1,MissingCount=1};
            var game=new CatalogGame {Id=54321,Name="Sample Game: Complete",
                Platforms=new List<string>{"Game Boy Advance"},Summary="Description",CoverImageId="co123"};
            var record=WorkspaceIdentityRules.ManualRecord(input,game,DateTime.UtcNow);
            Check(record.Status=="Review", "manual selection never strong");
            Check(record.Score=="", "no invented match score");
            Check(record.GameId==id && record.MissingMask==1 && record.MissingCount==1, "exact game and field gaps");
            Check(record.CatalogId=="54321" && record.Candidate==game.Name, "exact fetched ID and title");
            Check(record.AvailableMetadata.Contains("description") && record.AvailableMetadata.Contains("cover"), "field hints only");
            Check(record.Evidence.Contains("Not automatic"), "manual evidence disclaimer");
            Check(WorkspaceIdentityRules.SameSaved(record,"54321",game.Name,input), "identity unchanged");
            Check(!WorkspaceIdentityRules.SameSaved(record,"54322",game.Name,input), "changed ID rejected");
            Check(!WorkspaceIdentityRules.SameSaved(record,"54321","Sample Game",input), "changed title rejected");
            Check(!WorkspaceIdentityRules.SameSaved(record,"54321",game.Name,
                new CatalogInput {Id=id,Name=input.Name,Platform="Nintendo 3DS",MissingMask=1,MissingCount=1}),
                "platform change rejected");
            Check(!WorkspaceIdentityRules.SameSaved(record,"54321",game.Name,
                new CatalogInput {Id=id,Name=input.Name,Platform=input.Platform,MissingMask=2,MissingCount=1}),
                "missing fields change rejected");
            Check(!WorkspaceIdentityRules.SameSaved(record,"54321",game.Name,
                new CatalogInput {Id=Guid.NewGuid(),Name=input.Name,Platform=input.Platform,MissingMask=1,MissingCount=1}),
                "game ID change rejected");
            Check(!WorkspaceIdentityRules.SameSaved(record,"54321",game.Name,
                new CatalogInput {Id=id,Name=input.Name+"2",Platform=input.Platform,MissingMask=1,MissingCount=1}),
                "Playnite title change rejected");
            Check(!WorkspaceIdentityRules.SameSaved(null,"54321",game.Name,input), "absent cache rejected");
            try {WorkspaceIdentityRules.ManualRecord(input,new CatalogGame{Id=0,Name="No"},DateTime.UtcNow);throw new Exception("Invalid ID accepted");}
            catch (ArgumentException) {n++;}
            try {WorkspaceIdentityRules.ManualRecord(input,game,DateTime.Now);throw new Exception("Non-UTC accepted");}
            catch (ArgumentException) {n++;}
            string root=Path.Combine(Path.GetTempPath(),"SMM-Identity-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var cache=new CatalogScanCache(root);
                cache.SaveBatch(new[]{record},DateTime.UtcNow);
                var confirmations=new CatalogConfirmedIdentities(root);
                confirmations.Confirm(id,input.Name,input.Platform,54321,game.Name,DateTime.UtcNow);
                var service=new WorkspaceIdentityReview(root);
                var different=new CatalogGame{Id=54322,Name="Different edition"};
                service.StageChoice(input,"54321",game.Name,different);
                cache.Load();CatalogScanRecord updated;
                Check(cache.TryGet(input,DateTime.UtcNow,out updated) && updated.CatalogId=="54322", "manual choice cached");
                Check(updated.Status=="Review" && updated.Score=="", "corrected choice not auto eligible");
                confirmations.Load();
                Check(!confirmations.Has(id), "old confirmation forgotten on correction");
                try {service.Confirm(input,"54322",different.Name,new CatalogGame{Id=54322,Name="Changed on IGDB"});
                    throw new Exception("Changed title confirmed");}
                catch (InvalidOperationException) {n++;}
                service.Confirm(input,"54322",different.Name,different);
                confirmations.Load();
                Check(confirmations.TryMatch(id,input.Name,input.Platform,54322,different.Name),"confirmed exact ID and title");
                service.Forget(input,"54322",different.Name);
                confirmations.Load();Check(!confirmations.Has(id),"explicit forget");
                try {service.StageChoice(input,"54321",game.Name,game);throw new Exception("Stale identity accepted");}
                catch (InvalidOperationException) {n++;}
            }
            finally {if(Directory.Exists(root))Directory.Delete(root,true);}
            Console.WriteLine("PASS: "+n+" native manual identity and cache/confirmation safety assertions.");
        }
    }
}
