using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // Crash-detection ledger separate from the field-level enrichment undo journal.
    // Begin is flushed BEFORE invoking Playnite AddFile. Each returned ID is flushed
    // immediately. HandOff is allowed only after EnrichmentJournal.Prepare succeeds.
    // An interruption before the ID is returned/recorded cannot be auto-reconciled:
    // the open Begin must block future imports until explicit recovery.
    internal sealed class CatalogMediaStage
    {
        internal Guid OperationId;
        internal Guid GameId;
        internal readonly Dictionary<string,string> Media = new Dictionary<string,string>(StringComparer.Ordinal);
        internal bool Closed;
        internal string Outcome;
    }

    internal sealed class CatalogMediaStagingJournal
    {
        private const int MaxJournalBytes=16*1024*1024;
        private const int MaxMediaIdBytes=4096;
        private readonly object sync=new object();
        private readonly string path;
        internal CatalogMediaStagingJournal(string privateDataDirectory)
        {
            if(String.IsNullOrWhiteSpace(privateDataDirectory))
                throw new ArgumentException("Private plugin directory is required.");
            path=Path.Combine(privateDataDirectory,"media-staging-v1.log");
        }
        private static bool ValidSlot(string slot)
        {
            return slot=="cover" || slot=="icon" || slot=="background";
        }
        private List<CatalogMediaStage> LoadUnsafe()
        {
            var result=new List<CatalogMediaStage>();
            if(!File.Exists(path))return result;
            if(new FileInfo(path).Length>MaxJournalBytes)
                throw new InvalidDataException("Media staging ledger exceeds its size limit; no new imports allowed.");
            var byId=new Dictionary<Guid,CatalogMediaStage>();
            foreach(var raw in File.ReadAllLines(path,Encoding.UTF8))
            {
                if(String.IsNullOrWhiteSpace(raw))continue;
                var fields=raw.Split('|');
                Guid op,game;
                if(fields.Length<2 || !Guid.TryParseExact(fields[1],"N",out op) || op==Guid.Empty)
                    throw new InvalidDataException("Malformed media staging operation; imports blocked.");
                CatalogMediaStage current;
                if(fields[0]=="B" && fields.Length==3)
                {
                    if(!Guid.TryParseExact(fields[2],"N",out game) || game==Guid.Empty || byId.ContainsKey(op))
                        throw new InvalidDataException("Invalid/duplicate media staging begin.");
                    current=new CatalogMediaStage{OperationId=op,GameId=game};
                    result.Add(current);byId.Add(op,current);
                }
                else if(byId.TryGetValue(op,out current) && !current.Closed)
                {
                    if(fields[0]=="M" && fields.Length==4 && ValidSlot(fields[2]) &&
                        !current.Media.ContainsKey(fields[2]))
                    {
                        byte[] bytes;
                        try{bytes=Convert.FromBase64String(fields[3]);}
                        catch(FormatException ex){throw new InvalidDataException("Invalid staged media ID.",ex);}
                        if(bytes.Length==0 || bytes.Length>MaxMediaIdBytes)
                            throw new InvalidDataException("Invalid staged media ID length.");
                        var value=new UTF8Encoding(false,true).GetString(bytes);
                        if(String.IsNullOrWhiteSpace(value) || value.IndexOf('\0')>=0)
                            throw new InvalidDataException("Invalid staged media reference.");
                        current.Media.Add(fields[2],value);
                    }
                    else if((fields[0]=="H" || fields[0]=="A") && fields.Length==2)
                    {current.Closed=true;current.Outcome=fields[0];}
                    else throw new InvalidDataException("Out-of-sequence media staging event; imports blocked.");
                }
                else throw new InvalidDataException("Unknown media staging operation; imports blocked.");
            }
            return result;
        }
        private void AppendUnsafe(string entry)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using(var stream=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read))
            {
                var data=new UTF8Encoding(false).GetBytes(entry+"\n");
                stream.Write(data,0,data.Length);
                stream.Flush(true);
            }
        }
        internal List<CatalogMediaStage> Unresolved()
        {
            lock(sync) { return LoadUnsafe().Where(x=>!x.Closed).ToList(); }
        }
        internal void Begin(Guid operationId,Guid gameId)
        {
            if(operationId==Guid.Empty || gameId==Guid.Empty)throw new ArgumentException("Invalid media staging identity.");
            lock(sync)
            {
                var entries=LoadUnsafe();
                if(entries.Any(x=>!x.Closed) || entries.Any(x=>x.OperationId==operationId))
                    throw new InvalidOperationException("Unresolved or duplicate media staging operation; recovery required.");
                AppendUnsafe("B|"+operationId.ToString("N")+"|"+gameId.ToString("N"));
            }
        }
        internal void Track(Guid operationId,string slot,string mediaId)
        {
            if(operationId==Guid.Empty || !ValidSlot(slot) || String.IsNullOrWhiteSpace(mediaId))
                throw new ArgumentException("Invalid staged media entry.");
            var data=new UTF8Encoding(false,true).GetBytes(mediaId);
            if(data.Length>MaxMediaIdBytes || mediaId.IndexOf('\0')>=0)
                throw new ArgumentException("Staged media ID too long/invalid.");
            lock(sync)
            {
                var stage=LoadUnsafe().SingleOrDefault(x=>x.OperationId==operationId && !x.Closed);
                if(stage==null || stage.Media.ContainsKey(slot))
                    throw new InvalidOperationException("Media staging operation is not active or slot is already tracked.");
                AppendUnsafe("M|"+operationId.ToString("N")+"|"+slot+"|"+Convert.ToBase64String(data));
            }
        }
        private void Finish(Guid operationId,string outcome)
        {
            lock(sync)
            {
                var stage=LoadUnsafe().SingleOrDefault(x=>x.OperationId==operationId && !x.Closed);
                if(stage==null)throw new InvalidOperationException("Media staging operation is not active.");
                AppendUnsafe(outcome+"|"+operationId.ToString("N"));
            }
        }
        // Call only AFTER durable field journal Prepare; the undo record then owns the media IDs.
        internal void HandOff(Guid operationId){Finish(operationId,"H");}
        // Call only after all tracked media were removed, before field-journal Prepare.
        internal void AbandonClean(Guid operationId){Finish(operationId,"A");}
    }
}
