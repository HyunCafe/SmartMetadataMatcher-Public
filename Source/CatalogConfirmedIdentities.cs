using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Globalization;

namespace SmartMetadataMatcher
{
    // An explicit user decision about one game/IGDB identity. Not a write
    // authorization, similarity score, or platform proof. Never stores ROM paths.
    [DataContract]
    internal sealed class ConfirmedIdentity
    {
        [DataMember] public Guid GameId;
        [DataMember] public string GameName;
        [DataMember] public string Platform;
        [DataMember] public long IgdbId;
        [DataMember] public string CatalogTitle;
        [DataMember] public string ConfirmedUtc;
    }

    [DataContract]
    internal sealed class ConfirmedIdentityDocument
    {
        [DataMember] public int Version=1;
        [DataMember] public List<ConfirmedIdentity> Records=new List<ConfirmedIdentity>();
    }

    internal sealed class CatalogConfirmedIdentities
    {
        private const int MaxBytes=2*1024*1024;
        private const int MaxRecords=10000;
        private readonly string path;
        private readonly Dictionary<Guid,ConfirmedIdentity> records=new Dictionary<Guid,ConfirmedIdentity>();
        internal CatalogConfirmedIdentities(string pluginDataDir)
        {
            if(String.IsNullOrWhiteSpace(pluginDataDir))throw new ArgumentException("Missing private plugin data folder");
            path=Path.Combine(pluginDataDir,"confirmed-igdb-identities-v1.json");
        }
        private static void Validate(ConfirmedIdentity item)
        {
            DateTime stamp;
            if(item==null || item.GameId==Guid.Empty || item.IgdbId<=0 ||
                String.IsNullOrWhiteSpace(item.GameName) || item.GameName.Length>1024 ||
                item.Platform==null || item.Platform.Length>256 ||
                String.IsNullOrWhiteSpace(item.CatalogTitle) || item.CatalogTitle.Length>1024 ||
                !DateTime.TryParseExact(item.ConfirmedUtc,"o",CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,out stamp) || stamp.Kind!=DateTimeKind.Utc || stamp.Year<2020)
                throw new InvalidDataException("Invalid saved identity; existing decisions were not modified.");
        }
        internal void Load()
        {
            records.Clear();
            if(!File.Exists(path))return;
            if(new FileInfo(path).Length>MaxBytes)throw new InvalidDataException("Confirmed identity file exceeds size limit.");
            ConfirmedIdentityDocument document;
            try
            {
                using(var file=File.OpenRead(path))
                    document=(ConfirmedIdentityDocument)new DataContractJsonSerializer(typeof(ConfirmedIdentityDocument)).ReadObject(file);
            }
            catch(SerializationException ex){throw new InvalidDataException("Confirmed identity file is unreadable.",ex);}
            catch(System.Xml.XmlException ex){throw new InvalidDataException("Confirmed identity file cannot be parsed.",ex);}
            if(document==null || document.Version!=1 || document.Records==null || document.Records.Count>MaxRecords)
                throw new InvalidDataException("Unsupported confirmed identity format.");
            var parsed=new Dictionary<Guid,ConfirmedIdentity>();
            foreach(var record in document.Records)
            {
                Validate(record);
                if(parsed.ContainsKey(record.GameId))throw new InvalidDataException("Duplicate confirmed game ID.");
                parsed.Add(record.GameId,record);
            }
            foreach(var record in parsed)records.Add(record.Key,record.Value);
        }
        internal bool TryMatch(Guid gameId,string gameName,string platform,long igdbId,string catalogTitle)
        {
            ConfirmedIdentity record;
            return records.TryGetValue(gameId,out record) &&
                String.Equals(record.GameName,gameName,StringComparison.Ordinal) &&
                String.Equals(record.Platform,platform,StringComparison.Ordinal) &&
                record.IgdbId==igdbId &&
                String.Equals(record.CatalogTitle,catalogTitle,StringComparison.Ordinal);
        }
        internal bool Has(Guid gameId){return records.ContainsKey(gameId);}
        internal void Confirm(Guid gameId,string name,string platform,long igdbId,string title,DateTime nowUtc)
        {
            if(nowUtc.Kind!=DateTimeKind.Utc)throw new ArgumentException("UTC required");
            var record=new ConfirmedIdentity{GameId=gameId,GameName=name,Platform=platform,
                IgdbId=igdbId,CatalogTitle=title,
                ConfirmedUtc=nowUtc.ToString("o",CultureInfo.InvariantCulture)};
            Validate(record);
            var updated=new Dictionary<Guid,ConfirmedIdentity>(records);
            updated[gameId]=record;
            Write(updated);
        }
        internal void Forget(Guid gameId)
        {
            if(!records.ContainsKey(gameId))return;
            var updated=new Dictionary<Guid,ConfirmedIdentity>(records);
            updated.Remove(gameId);Write(updated);
        }
        private void Write(Dictionary<Guid,ConfirmedIdentity> updated)
        {
            if(updated.Count>MaxRecords)throw new InvalidDataException("Too many identities.");
            var doc=new ConfirmedIdentityDocument{Records=updated.Values.OrderBy(x=>x.GameId).ToList()};
            byte[] data;
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(ConfirmedIdentityDocument)).WriteObject(stream,doc);
                data=stream.ToArray();
            }
            if(data.Length>MaxBytes)throw new InvalidDataException("Confirmed identity file exceeds size limit.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllBytes(temporary,data);
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
                records.Clear();foreach(var item in updated)records.Add(item.Key,item.Value);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
