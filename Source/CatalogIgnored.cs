using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // Local private preference only: stores Playnite IDs, never titles, paths or credentials.
    // This list affects only SMM discovery, not the user's Playnite library records.
    internal sealed class CatalogIgnored
    {
        private readonly string path;
        internal CatalogIgnored(string pluginDataDir)
        {
            if(String.IsNullOrWhiteSpace(pluginDataDir))
                throw new ArgumentException("A private plugin data directory is required.");
            path=Path.Combine(pluginDataDir,"catalog-ignored-ids.txt");
        }
        internal HashSet<Guid> Load()
        {
            var ids=new HashSet<Guid>();
            if(!File.Exists(path))return ids;
            foreach(var line in File.ReadAllLines(path,Encoding.UTF8))
            {
                if(String.IsNullOrWhiteSpace(line))continue;
                Guid id;
                if(!Guid.TryParseExact(line.Trim(),"N",out id) || id==Guid.Empty)
                    throw new InvalidDataException("Invalid ignored-game ID. No changes were made.");
                ids.Add(id);
            }
            return ids;
        }
        internal void Save(IEnumerable<Guid> ids)
        {
            if(ids==null)throw new ArgumentNullException("ids");
            var unique=ids.Where(id=>id!=Guid.Empty).Distinct()
                .OrderBy(id=>id).Select(id=>id.ToString("N")).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary=path+".tmp";
            File.WriteAllLines(temporary,unique,new UTF8Encoding(false));
            try
            {
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}
