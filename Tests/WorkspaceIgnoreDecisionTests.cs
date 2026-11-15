using System;
using System.Collections.Generic;
using System.IO;
using SmartMetadataMatcher;

internal static class WorkspaceIgnoreDecisionTests
{
    internal static void Run()
    {
        var dir = Path.Combine(Path.GetTempPath(), "SMM-IgnoreDecision-" + Guid.NewGuid().ToString("N"));
        try
        {
            var game = Guid.NewGuid();
            var untouched = Guid.NewGuid();
            var store = new CatalogIgnored(dir);
            store.Save(new[] { untouched });
            WorkspaceIgnoreDecision.Set(dir, game, false, true);
            if (!store.Load().SetEquals(new[] { game, untouched }))
                throw new Exception("Ignore did not preserve unrelated IDs.");
            MustRefuse(() => WorkspaceIgnoreDecision.Set(dir, game, false, true),
                "Stale ignore decision must be refused.");
            MustRefuse(() => WorkspaceIgnoreDecision.Set(dir, game, true, true),
                "No-op ignore must be refused.");
            MustRefuse(() => WorkspaceIgnoreDecision.Set(dir, Guid.Empty, false, true),
                "Invalid game ID must be refused.");
            if (!store.Load().SetEquals(new[] { game, untouched }))
                throw new Exception("Rejected transitions changed ignored IDs.");
            WorkspaceIgnoreDecision.Set(dir, game, true, false);
            if (!store.Load().SetEquals(new[] { untouched }))
                throw new Exception("Restore did not retain unrelated ignored game.");
            MustRefuse(() => WorkspaceIgnoreDecision.Set(dir, game, true, false),
                "Stale restore decision must be refused.");
            // One atomic update for an entire filtered/selected batch.
            var first=Guid.NewGuid();
            var second=Guid.NewGuid();
            WorkspaceIgnoreDecision.SetMany(dir,new[]{first,second},false,true);
            if(!store.Load().SetEquals(new[]{first,second,untouched}))
                throw new Exception("Bulk ignore did not persist all IDs while preserving others.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new[]{first,game},false,true),
                "Mixed saved ignored state must reject entire batch.");
            if(!store.Load().SetEquals(new[]{first,second,untouched}))
                throw new Exception("Rejected mixed batch made partial changes.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new[]{first,first},true,false),
                "Duplicate IDs must be rejected.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new[]{first,Guid.Empty},true,false),
                "Empty ID must be rejected.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new Guid[0],true,false),
                "Empty selection must be rejected.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new[]{first},true,true),
                "No-op transitions must be rejected.");
            WorkspaceIgnoreDecision.SetMany(dir,new[]{first,second},true,false);
            if(!store.Load().SetEquals(new[]{untouched}))
                throw new Exception("Bulk restore did not preserve unrelated ignored game.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new[]{first},true,false),
                "Stale batch restore must be rejected.");
            File.WriteAllText(Path.Combine(dir, "catalog-ignored-ids.txt"), "malformed record");
            var original = File.ReadAllBytes(Path.Combine(dir, "catalog-ignored-ids.txt"));
            MustRefuse(() => WorkspaceIgnoreDecision.Set(dir, game, false, true),
                "Malformed store must fail closed.");
            MustRefuse(()=>WorkspaceIgnoreDecision.SetMany(dir,new[]{first,second},false,true),
                "Bulk ignore must reject a malformed preference file.");
            var after = File.ReadAllBytes(Path.Combine(dir, "catalog-ignored-ids.txt"));
            if (original.Length != after.Length) throw new Exception("Malformed store overwritten.");
            for (int i = 0; i < original.Length; i++)
                if (original[i] != after[i]) throw new Exception("Malformed store modified.");
            Console.WriteLine("PASS: 19 individual and bulk ignore/restore preference-state and fail-closed checks.");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
    private static void MustRefuse(Action action, string label)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        catch (InvalidDataException) { return; }
        throw new Exception(label);
    }
}
