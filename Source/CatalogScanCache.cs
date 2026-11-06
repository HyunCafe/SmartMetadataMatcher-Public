using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SmartMetadataMatcher
{
    // Private read-only lookup continuity. No API credentials, game files,
    // Playnite metadata, or write authorization are stored in this cache.
    [DataContract]
    internal sealed class CatalogScanRecord
    {
        [DataMember] public Guid GameId;
        [DataMember] public string Name;
        [DataMember] public string Platform;
        [DataMember] public int MissingCount;
        [DataMember] public string ScannedUtc;
        [DataMember] public string Candidate;
        [DataMember] public string Status;
        [DataMember] public string Evidence;
        [DataMember] public string CatalogId;
        [DataMember] public string Score;
        [DataMember] public string AvailableMetadata;
        [DataMember] public string Summary;
        [DataMember] public string CoverId;
        [DataMember] public string ScreenshotId;
        [DataMember] public string Queries;
        [DataMember] public string Alternatives;
        [DataMember] public string ReleaseYear;
        [DataMember] public string FoundCount;
    }

    [DataContract]
    internal sealed class CatalogScanDocument
    {
        [DataMember] public int Version = 1;
        [DataMember] public List<CatalogScanRecord> Records = new List<CatalogScanRecord>();
    }

    internal sealed class CatalogScanCache
    {
        private const int MaxBytes = 8 * 1024 * 1024;
        private const int MaxRecords = 10000;
        internal static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
        private readonly string path;
        private readonly Dictionary<Guid,CatalogScanRecord> known = new Dictionary<Guid,CatalogScanRecord>();
        internal CatalogScanCache(string pluginDataDir)
        {
            if(String.IsNullOrWhiteSpace(pluginDataDir))
                throw new ArgumentException("Plugin data directory is required.");
            path=Path.Combine(pluginDataDir,"catalog-scans-v1.json");
        }

        private static bool HasValidUtcTime(string value)
        {
            DateTime parsed;
            return DateTime.TryParseExact(value,"o",CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,out parsed) && parsed.Kind==DateTimeKind.Utc &&
                parsed.Year>=2020;
        }
        private static DateTime ParseUtc(string value)
        {
            return DateTime.ParseExact(value,"o",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind);
        }
        private static void Validate(CatalogScanRecord entry)
        {
            if(entry==null || entry.GameId==Guid.Empty || String.IsNullOrWhiteSpace(entry.Name) ||
                entry.Platform==null || entry.Name.Length>1024 ||
                entry.Platform.Length>256 || entry.MissingCount<0 || entry.MissingCount>100 ||
                (entry.Status!="Strong candidate" && entry.Status!="Review" && entry.Status!="Unmatched") ||
                !HasValidUtcTime(entry.ScannedUtc) ||
                (entry.Candidate??"").Length>1024 || (entry.Evidence??"").Length>16000 ||
                (entry.Summary??"").Length>32000 || (entry.Alternatives??"").Length>16000)
                throw new InvalidDataException("Malformed saved scan result. Existing cache was not modified.");
        }

        internal void Load()
        {
            known.Clear();
            if(!File.Exists(path))return;
            var info=new FileInfo(path);
            if(info.Length>MaxBytes)throw new InvalidDataException("Saved scan cache exceeds size limit.");
            CatalogScanDocument document;
            try
            {
                using(var stream=File.OpenRead(path))
                    document=(CatalogScanDocument)new DataContractJsonSerializer(typeof(CatalogScanDocument)).ReadObject(stream);
            }
            catch(SerializationException ex)
            { throw new InvalidDataException("Saved scan cache is not valid JSON.",ex); }
            if(document==null || document.Version!=1 || document.Records==null ||
                document.Records.Count>MaxRecords)
                throw new InvalidDataException("Saved scan cache has an unsupported format.");
            var loaded=new Dictionary<Guid,CatalogScanRecord>();
            foreach(var record in document.Records)
            {
                Validate(record);
                if(loaded.ContainsKey(record.GameId))
                    throw new InvalidDataException("Duplicate saved game identifier in cache.");
                loaded.Add(record.GameId,record);
            }
            foreach(var item in loaded)known.Add(item.Key,item.Value);
        }

        internal bool TryGet(CatalogInput input,DateTime nowUtc,out CatalogScanRecord record)
        {
            record=null;
            if(input==null || input.Id==Guid.Empty || nowUtc.Kind!=DateTimeKind.Utc)return false;
            CatalogScanRecord candidate;
            if(!known.TryGetValue(input.Id,out candidate))return false;
            if(!String.Equals(candidate.Name,input.Name,StringComparison.Ordinal) ||
                !String.Equals(candidate.Platform,input.Platform,StringComparison.Ordinal) ||
                candidate.MissingCount!=input.MissingCount ||
                ParseUtc(candidate.ScannedUtc)>nowUtc.AddMinutes(5) ||
                ParseUtc(candidate.ScannedUtc)<nowUtc-Lifetime)return false;
            record=candidate;return true;
        }

        internal void SaveBatch(IEnumerable<CatalogScanRecord> batch,DateTime nowUtc)
        {
            if(batch==null)throw new ArgumentNullException("batch");
            if(nowUtc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC time required.");
            // Validate first, before changing the cache's in-memory state.
            var entries=batch.ToList();
            foreach(var record in entries)Validate(record);
            var snapshot=new Dictionary<Guid,CatalogScanRecord>(known);
            foreach(var record in entries)snapshot[record.GameId]=record;
            var retained=snapshot.Values.Where(x=>ParseUtc(x.ScannedUtc)>=nowUtc-Lifetime &&
                ParseUtc(x.ScannedUtc)<=nowUtc.AddMinutes(5)).OrderBy(x=>x.GameId).ToList();
            if(retained.Count>MaxRecords)throw new InvalidDataException("Saved scan cache reached size limit.");
            var data=new CatalogScanDocument{Version=1,Records=retained};
            byte[] content;
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(CatalogScanDocument)).WriteObject(stream,data);
                content=stream.ToArray();
            }
            if(content.Length>MaxBytes)throw new InvalidDataException("Saved scan cache reached size limit.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllBytes(temporary,content);
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
                known.Clear();
                foreach(var record in retained)known.Add(record.GameId,record);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
        }

        // Explicit fresh scan: preserve the prior snapshot for offline recovery.
        internal string ArchiveAndClear(DateTime nowUtc)
        {
            if(nowUtc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC time required.");
            string archived="";
            if(File.Exists(path))
            {
                archived=path+"."+nowUtc.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".bak";
                File.Move(path,archived);
            }
            known.Clear();return archived;
        }
    }
}
