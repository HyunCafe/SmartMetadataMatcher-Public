using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    internal sealed class MetadataEntry
    {
        internal Guid OperationId;
        internal Guid GameId;
        internal long IgdbId;
        internal string GameName;
        internal string BeforeDescription;
        internal string AfterDescription;
        internal string BeforeCover;
        internal string AfterCover;
        internal bool ChangedDescription;
        internal bool ChangedCover;
        internal bool Undone;
    }

    // A standalone, per-plugin append-only write-ahead journal. Never overwrites
    // the existing rename journal or Playnite's user data. P is flushed before
    // Games.Update; U marks safe undo. Interrupted P entries can be reconciled.
    internal sealed class MetadataJournal
    {
        private readonly string path;
        private static readonly object Sync = new object();
        internal MetadataJournal(string dataDirectory)
        {
            if (String.IsNullOrWhiteSpace(dataDirectory)) throw new ArgumentException("Missing private plugin data path");
            path = Path.Combine(dataDirectory, "metadata-journal-v1.log");
        }
        private static string Enc(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? "")); }
        private static string Dec(string s) { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); }
        private void Append(string value)
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    var bytes = new UTF8Encoding(false).GetBytes(value + "\n");
                    file.Write(bytes, 0, bytes.Length);
                    file.Flush(true);
                }
            }
        }
        internal void Prepare(MetadataEntry e)
        {
            if (e == null || e.OperationId == Guid.Empty || e.GameId == Guid.Empty || e.IgdbId <= 0 ||
                String.IsNullOrWhiteSpace(e.GameName) || (!e.ChangedDescription && !e.ChangedCover))
                throw new ArgumentException("Invalid metadata operation");
            // One line, escaped fields, no API credentials or private executable paths.
            Append(String.Join("|", new[] {"P", e.OperationId.ToString("N"), e.GameId.ToString("N"),
                e.IgdbId.ToString(System.Globalization.CultureInfo.InvariantCulture), Enc(e.GameName),
                Enc(e.BeforeDescription), Enc(e.AfterDescription), Enc(e.BeforeCover), Enc(e.AfterCover),
                e.ChangedDescription ? "1" : "0", e.ChangedCover ? "1" : "0"}));
        }
        internal void MarkUndone(Guid operationId)
        {
            if (operationId == Guid.Empty) throw new ArgumentException("Missing operation ID");
            Append("U|" + operationId.ToString("N"));
        }
        internal List<MetadataEntry> Pending()
        {
            lock (Sync)
            {
                var entries = new List<MetadataEntry>();
                if (!File.Exists(path)) return entries;
                foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (String.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split('|');
                    Guid op, game; long igdb;
                    if (parts[0] == "P" && parts.Length == 11 &&
                        Guid.TryParseExact(parts[1], "N", out op) &&
                        Guid.TryParseExact(parts[2], "N", out game) &&
                        Int64.TryParse(parts[3], out igdb) && op != Guid.Empty && game != Guid.Empty && igdb > 0 &&
                        (parts[9] == "0" || parts[9] == "1") && (parts[10] == "0" || parts[10] == "1"))
                    {
                        if (entries.Any(e => e.OperationId == op)) throw new InvalidDataException("Duplicate metadata operation in journal");
                        entries.Add(new MetadataEntry { OperationId=op, GameId=game, IgdbId=igdb,
                            GameName=Dec(parts[4]), BeforeDescription=Dec(parts[5]), AfterDescription=Dec(parts[6]),
                            BeforeCover=Dec(parts[7]), AfterCover=Dec(parts[8]),
                            ChangedDescription=parts[9]=="1", ChangedCover=parts[10]=="1" });
                    }
                    else if (parts[0] == "U" && parts.Length == 2 && Guid.TryParseExact(parts[1], "N", out op))
                    {
                        var found = entries.LastOrDefault(e => e.OperationId == op && !e.Undone);
                        if (found == null) throw new InvalidDataException("Unmatched metadata undo marker");
                        found.Undone=true;
                    }
                    else throw new InvalidDataException("Unrecognized or truncated metadata journal record; refusing modifications");
                }
                return entries.Where(e=>!e.Undone).AsEnumerable().Reverse().ToList();
            }
        }
        internal MetadataEntry LatestPending()
        {
            return Pending().FirstOrDefault();
        }
    }
}
