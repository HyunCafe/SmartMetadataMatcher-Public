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

    // Same history-hidden-operations.txt used by Classic. A visibility change
    // never edits the journal, changes undo ordering or modifies Playnite games.
    internal static class WorkspaceHistoryVisibility
    {
        internal static void Set(string dataPath, IEnumerable<Guid> operationIds, bool hide)
        {
            if(String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Plugin data directory is required.");
            var ids=(operationIds??Enumerable.Empty<Guid>()).ToList();
            if(ids.Count==0 || ids.Any(x=>x==Guid.Empty) ||
                ids.Distinct().Count()!=ids.Count)
                throw new InvalidOperationException("Select distinct valid history operations.");
            // Recheck the complete journal after the user's confirmation;
            // hidden entries are still present and remain undo-relevant.
            var journalIds=new HashSet<Guid>(new EnrichmentJournal(dataPath)
                .AllEntries().Select(x=>x.OperationId));
            if(ids.Any(id=>!journalIds.Contains(id)))
                throw new InvalidOperationException("History changed. Reopen History before editing visibility.");
            var store=new HistoryVisibilityStore(dataPath);
            var before=store.Load();
            if(ids.Any(id=>before.Contains(id)==hide))
                throw new InvalidOperationException("Visibility changed since review. Reopen History.");
            var updated=new HashSet<Guid>(before);
            foreach(var id in ids) {if(hide) updated.Add(id); else updated.Remove(id);}
            store.Save(updated);  // Reuse existing atomic preference writer.
            if(!store.Load().SetEquals(updated))
                throw new IOException("History visibility was not verified after saving.");
        }
    }
}
