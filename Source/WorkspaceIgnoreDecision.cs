using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartMetadataMatcher
{
    // One reusable boundary for the existing ignored-ID preference store.
    // This does not touch the Playnite game database, scan cache, or import journals.
    internal static class WorkspaceIgnoreDecision
    {
        internal static void Set(string dataPath, Guid gameId, bool expectedIgnored, bool ignore)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Plugin data path is required.");
            if (gameId == Guid.Empty || expectedIgnored == ignore)
                throw new InvalidOperationException("No valid ignored-state transition was approved.");
            var store = new CatalogIgnored(dataPath);
            var before = store.Load(); // malformed / unreadable data fails closed
            if (before.Contains(gameId) != expectedIgnored)
                throw new InvalidOperationException("Ignored status changed since approval. Reload and retry.");
            var revised = new HashSet<Guid>(before);
            if (ignore) revised.Add(gameId);
            else revised.Remove(gameId);
            store.Save(revised); // existing durable/atomic replace, not a second store
            var saved = store.Load();
            if (!saved.SetEquals(revised))
                throw new IOException("Ignored state could not be verified. Reopen to inspect saved status.");
        }

        // All-or-nothing batch preference change. No Playnite database or import journal writes.
        // Every selected game must still have the expected ignored state after approval.
        internal static void SetMany(string dataPath, IEnumerable<Guid> gameIds,
            bool expectedIgnored, bool ignore)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Plugin data path is required.");
            if (expectedIgnored == ignore || gameIds == null)
                throw new InvalidOperationException("Invalid bulk ignored-state transition.");
            var ids = gameIds.ToList();
            if (ids.Count == 0 || ids.Any(x => x == Guid.Empty) ||
                ids.Distinct().Count() != ids.Count)
                throw new InvalidOperationException("Select distinct, valid Playnite games.");
            var store = new CatalogIgnored(dataPath);
            var before = store.Load(); // Reject malformed or unreadable preferences.
            if (ids.Any(id => before.Contains(id) != expectedIgnored))
                throw new InvalidOperationException("An ignored game changed since review. Reload and retry.");
            var next = new HashSet<Guid>(before);
            foreach (var id in ids)
            {
                if (ignore) next.Add(id);
                else next.Remove(id);
            }
            store.Save(next); // Exactly one atomic existing-store write per batch.
            if (!store.Load().SetEquals(next))
                throw new IOException("Saved ignored states could not be verified. Reopen before retrying.");
        }
    }
}
