using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // A private PRESENTATION preference: hiding an entry never deletes the
    // write-ahead journal, its undo data, the Playnite game, or its media.
    internal sealed class HistoryVisibilityStore
    {
        private readonly string path;
        internal HistoryVisibilityStore(string pluginDataDir)
        {
            if(String.IsNullOrWhiteSpace(pluginDataDir))
                throw new ArgumentException("Plugin data directory is required.");
            path=Path.Combine(pluginDataDir,"history-hidden-operations.txt");
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
                    throw new InvalidDataException("Invalid hidden history identifier; preferences unchanged.");
                ids.Add(id);
            }
            return ids;
        }
        internal void Save(IEnumerable<Guid> ids)
        {
            if(ids==null)throw new ArgumentNullException("ids");
            var values=ids.Where(id=>id!=Guid.Empty).Distinct().OrderBy(x=>x)
                .Select(x=>x.ToString("N")).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllLines(temp,values,new UTF8Encoding(false));
                if(File.Exists(path))File.Replace(temp,path,null);
                else File.Move(temp,path);
            }
            finally {if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
