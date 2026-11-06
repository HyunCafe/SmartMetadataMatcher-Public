using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SmartMetadataMatcher
{
    // Separate v2 journal: the existing v0.4.0 description/cover journal remains
    // readable and undoable. The v2 log tracks ALL changed fields in one operation.
    [DataContract]
    internal sealed class EnrichmentEntry
    {
        [DataMember] public Guid OperationId;
        [DataMember] public Guid GameId;
        [DataMember] public long IgdbId;
        [DataMember] public string CatalogTitle;
        [DataMember] public string BeforeName, AfterName;
        [DataMember] public string BeforeSorting, AfterSorting;
        [DataMember] public string BeforeDescription, AfterDescription;
        [DataMember] public string BeforeCover, AfterCover;
        [DataMember] public string BeforeIcon, AfterIcon;
        [DataMember] public string BeforeBackground, AfterBackground;
        [DataMember] public bool ChangedName, ChangedSorting, ChangedDescription;
        [DataMember] public bool ChangedCover, ChangedIcon, ChangedBackground;
        [DataMember] public string BeforeRelease, AfterRelease;
        [DataMember] public List<Guid> BeforeGenres, AfterGenres;
        [DataMember] public List<Guid> BeforeDevelopers, AfterDevelopers;
        [DataMember] public List<Guid> BeforePublishers, AfterPublishers;
        [DataMember] public List<Guid> BeforeSeries, AfterSeries;
        [DataMember] public bool ChangedRelease, ChangedGenres, ChangedDevelopers,
            ChangedPublishers, ChangedSeries;
        [DataMember] public string Note;
        public bool Committed;
        public bool Undone;
    }

    // P = write-ahead intent, C = confirmed DB write, U = undo completed.
    // All records are flushed. An interrupted P remains undoable after restart.
    internal sealed class EnrichmentJournal
    {
        private readonly string path;
        private readonly object sync = new object();
        internal EnrichmentJournal(string dataDir)
        {
            if (String.IsNullOrWhiteSpace(dataDir)) throw new ArgumentException("Missing private plugin data directory");
            path=Path.Combine(dataDir,"enrichment-journal-v2.log");
        }
        private static string Serialize(EnrichmentEntry entry)
        {
            var ser=new DataContractJsonSerializer(typeof(EnrichmentEntry));
            using (var ms=new MemoryStream())
            {
                ser.WriteObject(ms,entry);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        private static EnrichmentEntry Deserialize(string encoded)
        {
            var ser=new DataContractJsonSerializer(typeof(EnrichmentEntry));
            using (var ms=new MemoryStream(Convert.FromBase64String(encoded)))
                return (EnrichmentEntry)ser.ReadObject(ms);
        }
        private void Append(string record)
        {
            lock(sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using(var fs=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read))
                {
                    var bytes=new UTF8Encoding(false).GetBytes(record+"\n");
                    fs.Write(bytes,0,bytes.Length);
                    fs.Flush(true);
                }
            }
        }
        internal void Prepare(EnrichmentEntry entry)
        {
            if (entry==null || entry.OperationId==Guid.Empty || entry.GameId==Guid.Empty || entry.IgdbId<=0 ||
                !(entry.ChangedName || entry.ChangedSorting || entry.ChangedDescription ||
                  entry.ChangedCover || entry.ChangedIcon || entry.ChangedBackground ||
                  entry.ChangedRelease || entry.ChangedGenres || entry.ChangedDevelopers ||
                  entry.ChangedPublishers || entry.ChangedSeries))
                throw new ArgumentException("No valid enrichment change to journal");
            Append("P|"+Serialize(entry));
        }
        internal void Commit(Guid op) { if(op==Guid.Empty) throw new ArgumentException("Invalid operation"); Append("C|"+op.ToString("N")); }
        internal void MarkUndone(Guid op) { if(op==Guid.Empty) throw new ArgumentException("Invalid operation"); Append("U|"+op.ToString("N")); }
        internal List<EnrichmentEntry> OpenEntries()
        {
            lock(sync)
            {
                var entries=new List<EnrichmentEntry>();
                if (!File.Exists(path)) return entries;
                foreach(var line in File.ReadAllLines(path,Encoding.UTF8))
                {
                    if(String.IsNullOrWhiteSpace(line)) continue;
                    var parts=line.Split('|'); Guid id;
                    if(parts.Length==2 && parts[0]=="P")
                    {
                        EnrichmentEntry e;
                        try { e=Deserialize(parts[1]); }
                        catch(Exception ex) { throw new InvalidDataException("Invalid enrichment journal entry",ex); }
                        if(e==null || e.OperationId==Guid.Empty || e.GameId==Guid.Empty || e.IgdbId<=0 ||
                           entries.Any(x=>x.OperationId==e.OperationId))
                            throw new InvalidDataException("Invalid/duplicate enrichment operation");
                        entries.Add(e);
                    }
                    else if(parts.Length==2 && Guid.TryParseExact(parts[1],"N",out id) &&
                            (parts[0]=="C" || parts[0]=="U"))
                    {
                        var e=entries.LastOrDefault(x=>x.OperationId==id && !x.Undone);
                        if(e==null) throw new InvalidDataException("Unmatched enrichment journal marker");
                        if(parts[0]=="C")
                        {
                            if(e.Committed) throw new InvalidDataException("Duplicate enrichment commit");
                            e.Committed=true;
                        }
                        else e.Undone=true;
                    }
                    else throw new InvalidDataException("Unrecognized/truncated enrichment journal; refusing writes");
                }
                entries.Reverse();
                return entries.Where(x=>!x.Undone).ToList();
            }
        }
        internal EnrichmentEntry LatestOpen() { return OpenEntries().FirstOrDefault(); }
        internal List<EnrichmentEntry> Committed() { return OpenEntries().Where(x=>x.Committed).ToList(); }
    }
}
