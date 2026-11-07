using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // Write-ahead log: every intended rename is saved BEFORE modifying the database.
    // The journal stays inside Playnite's private per-plugin data directory.
    internal sealed class JournalEntry
    {
        internal Guid BatchId;
        internal Guid GameId;
        internal string Before;
        internal string After;
        internal bool Undone;
    }

    internal sealed class RenameJournal
    {
        private readonly string path;
        private static readonly object SyncRoot = new object();

        internal RenameJournal(string pluginDataPath)
        {
            if (String.IsNullOrWhiteSpace(pluginDataPath)) throw new ArgumentException("Missing plugin-data directory");
            path = Path.Combine(pluginDataPath, "rename-journal-v1.log");
        }

        private static string Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? ""));
        }
        private static string Decode(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }
        private void Append(string line)
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    var bytes = new UTF8Encoding(false).GetBytes(line + "\n");
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
            }
        }
        internal void Prepare(Guid batchId, Guid gameId, string before, string after)
        {
            if (batchId == Guid.Empty || gameId == Guid.Empty || String.IsNullOrWhiteSpace(before) ||
                String.IsNullOrWhiteSpace(after) || String.Equals(before, after, StringComparison.Ordinal))
                throw new ArgumentException("Invalid rename journal entry");
            Append("P|" + batchId.ToString("N") + "|" + gameId.ToString("N") + "|" + Encode(before) + "|" + Encode(after));
        }
        internal void MarkUndone(Guid batchId, Guid gameId)
        {
            Append("U|" + batchId.ToString("N") + "|" + gameId.ToString("N"));
        }
        internal List<JournalEntry> Pending()
        {
            lock (SyncRoot)
            {
                var items = new List<JournalEntry>();
                if (!File.Exists(path)) return items;
                foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    var part = line.Split('|');
                    Guid batch, id;
                    if (part.Length < 3 || !Guid.TryParseExact(part[1], "N", out batch) ||
                        !Guid.TryParseExact(part[2], "N", out id))
                        throw new InvalidDataException("Rename journal contains an invalid record. Refusing undo.");
                    if (part[0] == "P" && part.Length == 5)
                    {
                        var before = Decode(part[3]);
                        var after = Decode(part[4]);
                        if (String.IsNullOrWhiteSpace(before) || String.IsNullOrWhiteSpace(after))
                            throw new InvalidDataException("Rename journal has an empty title.");
                        items.Add(new JournalEntry { BatchId = batch, GameId = id, Before = before, After = after });
                    }
                    else if (part[0] == "U" && part.Length == 3)
                    {
                        var last = items.LastOrDefault(e => e.BatchId == batch && e.GameId == id && !e.Undone);
                        if (last == null) throw new InvalidDataException("Undo marker has no corresponding rename.");
                        last.Undone = true;
                    }
                    else throw new InvalidDataException("Unrecognized rename journal line. Refusing undo.");
                }
                // A game can be renamed repeatedly; only its newest active change is eligible first.
                var seen = new HashSet<Guid>();
                return items.Where(e => !e.Undone).Reverse().Where(e => seen.Add(e.GameId)).ToList();
            }
        }
        internal List<JournalEntry> LatestPendingBatch()
        {
            var entries = Pending();
            if (entries.Count == 0) return entries;
            var batch = entries[0].BatchId;
            return entries.Where(e => e.BatchId == batch).ToList();
        }
    }
}
