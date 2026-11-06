using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    internal sealed class GameExtrasSnapshot
    {
        internal string Release;
        internal List<Guid> Genres,Developers,Publishers,Series;
        internal static string ReleaseValue(Game game)
        {
            return game.ReleaseDate.HasValue ? game.ReleaseDate.Value.Serialize() : "";
        }
        internal static GameExtrasSnapshot Capture(Game g)
        {
            return new GameExtrasSnapshot{
                Release=ReleaseValue(g),
                Genres=(g.GenreIds??new List<Guid>()).ToList(),
                Developers=(g.DeveloperIds??new List<Guid>()).ToList(),
                Publishers=(g.PublisherIds??new List<Guid>()).ToList(),
                Series=(g.SeriesIds??new List<Guid>()).ToList()
            };
        }
        internal bool Matches(Game g)
        {
            return EnrichmentPlan.IsExpected(Release,ReleaseValue(g)) &&
                EnrichmentPlan.SameIds(Genres,g.GenreIds) &&
                EnrichmentPlan.SameIds(Developers,g.DeveloperIds) &&
                EnrichmentPlan.SameIds(Publishers,g.PublisherIds) &&
                EnrichmentPlan.SameIds(Series,g.SeriesIds);
        }
    }

    // A committed change and a verified no-op are different outcomes. A no-op
    // has no field journal or undo operation, and is returned only after the
    // staging ledger has been durably closed as clean.
    internal sealed class CatalogApplyResult
    {
        internal EnrichmentEntry Entry { get; private set; }
        internal bool NoChanges { get { return Entry==null; } }
        private CatalogApplyResult(EnrichmentEntry entry){Entry=entry;}
        internal static CatalogApplyResult Applied(EnrichmentEntry entry)
        {
            if(entry==null)throw new ArgumentNullException("entry");
            return new CatalogApplyResult(entry);
        }
        internal static CatalogApplyResult Skipped(){return new CatalogApplyResult(null);}
    }

    // All Playnite writes occur on its UI dispatcher. Existing values remain untouched.
    // Existing metadata, game files, emulators and Playnite actions are never changed.
    internal static class CatalogEnrichmentWriter
    {
        private static string Platforms(Game g)
        {
            return g.Platforms==null ? "" : String.Join(", ",g.Platforms.Select(p=>p.Name));
        }
        private static string SaveImage(IPlayniteAPI api, Guid gameId, byte[] jpg)
        {
            if(!MetadataPlan.LooksLikeJpeg(jpg)) return "";
            var temp=Path.Combine(Path.GetTempPath(),"SMM-IGDB-"+Guid.NewGuid().ToString("N")+".jpg");
            try
            {
                File.WriteAllBytes(temp,jpg);
                var id=api.Database.AddFile(temp,gameId);
                if(String.IsNullOrWhiteSpace(id)) throw new IOException("Playnite did not return an artwork ID.");
                return id;
            }
            finally { try { if(File.Exists(temp)) File.Delete(temp); } catch { } }
        }
        internal static CatalogApplyResult ApplyOne(IPlayniteAPI api, EnrichmentJournal journal,Guid gameId,
            string expectedName,string expectedPlatform,string expectedDescription,string expectedCover,
            string expectedIcon,string expectedBackground,string expectedSorting,CatalogGame catalog,
            byte[] cover,byte[] icon,byte[] background,bool rename,GameExtrasSnapshot expectedExtras,
            string approvedDisplayTitle,CatalogImportDraft draft=null)
        {
            if(catalog==null || catalog.Id<=0) throw new InvalidOperationException("Missing verified catalog ID.");
            if(expectedExtras==null) throw new InvalidOperationException("Missing original metadata snapshot.");
            // Fail closed on damaged journal before modifying the library.
            var recovery=CatalogRecoveryConsistency.Inspect(
                journal.MediaStaging.AllStages(),journal.AllEntries());
            if(!recovery.AllowsWrites)
                throw new InvalidOperationException("A metadata/artwork recovery check failed: " +
                    recovery.Problems[0]);
            var game=api.Database.Games[gameId];
            if(game==null) throw new InvalidOperationException("The Playnite game no longer exists.");
            if(!EnrichmentPlan.IsExpected(game.Name,expectedName) ||
               !EnrichmentPlan.IsExpected(game.Description,expectedDescription) ||
               !EnrichmentPlan.IsExpected(game.CoverImage,expectedCover) ||
               !EnrichmentPlan.IsExpected(game.Icon,expectedIcon) ||
               !EnrichmentPlan.IsExpected(game.BackgroundImage,expectedBackground) ||
               !EnrichmentPlan.IsExpected(game.SortingName,expectedSorting) ||
               !String.Equals(Platforms(game),expectedPlatform,StringComparison.Ordinal) ||
               !expectedExtras.Matches(game))
                throw new InvalidOperationException("The game changed after preview. Rescan before applying.");
            if(draft!=null && (!CatalogImportDraft.Valid(draft) ||
                !draft.Matches(gameId,expectedName,expectedPlatform,catalog.Id,catalog.Name)))
                throw new InvalidOperationException("The staged edit no longer matches this game and catalog identity; review again.");
            long platformId=CatalogCore.PlatformId(expectedPlatform);
            if(platformId<=0 || catalog.PlatformIds==null || !catalog.PlatformIds.Contains(platformId))
                throw new InvalidOperationException("IGDB platform does not corroborate this Playnite game.");
            var e=new EnrichmentEntry {
                OperationId=Guid.NewGuid(), GameId=game.Id, IgdbId=catalog.Id, CatalogTitle=catalog.Name,
                BeforeName=game.Name??"", BeforeSorting=game.SortingName??"",
                BeforeDescription=game.Description??"", BeforeCover=game.CoverImage??"",
                BeforeIcon=game.Icon??"", BeforeBackground=game.BackgroundImage??"",
                BeforeRelease=expectedExtras.Release,
                BeforeGenres=expectedExtras.Genres.ToList(),
                BeforeDevelopers=expectedExtras.Developers.ToList(),
                BeforePublishers=expectedExtras.Publishers.ToList(),
                BeforeSeries=expectedExtras.Series.ToList()
            };
            e.AfterName=e.BeforeName; e.AfterSorting=e.BeforeSorting;
            e.AfterDescription=e.BeforeDescription; e.AfterCover=e.BeforeCover;
            e.AfterIcon=e.BeforeIcon; e.AfterBackground=e.BeforeBackground;
            e.AfterRelease=e.BeforeRelease;
            e.AfterGenres=e.BeforeGenres.ToList(); e.AfterDevelopers=e.BeforeDevelopers.ToList();
            e.AfterPublishers=e.BeforePublishers.ToList(); e.AfterSeries=e.BeforeSeries.ToList();
            // During the SAME approved metadata import, clean only Playnite's
            // display title. Never change ROM filenames, launch paths or actions.
            // This uses the same pure planner shown to the user before approval.
            if(rename || (draft!=null && draft.CustomTitleEnabled))
            {
                var names=api.Database.Games.Where(g=>g!=null).Select(g=>
                    Tuple.Create(g.Id,g.Name,Platforms(g))).ToList();
                var wanted=CatalogImportDraft.ResolveTitle(
                    game.Name,catalog.Name,game.Id,expectedPlatform,names,rename,draft);
                if(wanted==null)
                    throw new InvalidOperationException("The edited title is invalid or conflicts with another Playnite game.");
                if(String.Equals(game.Name,wanted,StringComparison.Ordinal) && rename &&
                   (draft==null || !draft.CustomTitleEnabled))
                    e.Note="No unambiguous replacement title available; metadata may still be updated.";
                else if(!String.Equals(game.Name,wanted,StringComparison.Ordinal))
                {
                    e.AfterName=wanted; e.ChangedName=true;
                    if(EnrichmentPlan.MaySetSorting(game.SortingName,game.Name) &&
                       !String.IsNullOrWhiteSpace(game.SortingName))
                    {
                        e.AfterSorting=e.AfterName; e.ChangedSorting=true;
                    }
                }
            }
            // The plan is reviewed BEFORE remote download or any file/database
            // staging. Fail closed if earlier batch changes altered this title.
            if(!String.Equals(e.AfterName,approvedDisplayTitle,StringComparison.Ordinal))
                throw new InvalidOperationException("Approved Playnite title differs from current plan; rescan before applying.");
            if(draft!=null && draft.CustomDescriptionEnabled)
            {
                if(!CatalogImportDraft.SafeDescription(draft.CustomDescription))
                    throw new InvalidOperationException("The custom description is empty or too long.");
                e.AfterDescription=MetadataPlan.HtmlFromSummary(draft.CustomDescription);
                e.ChangedDescription=!EnrichmentPlan.IsExpected(e.BeforeDescription,e.AfterDescription);
            }
            else if((draft==null || draft.Description) &&
                    MetadataPlan.Missing(game.Description) && !String.IsNullOrWhiteSpace(catalog.Summary))
            {
                e.AfterDescription=MetadataPlan.HtmlFromSummary(catalog.Summary);
                e.ChangedDescription=true;
            }
            // Every imported image is tracked so undo can remove only files created by us.
            var imported=new List<string>();
            bool prepared=false;
            bool mediaUploadUncertain=false;
            bool sharedReferenceWriteAttempted=false;
            bool noChangeAbandonAttempted=false;
            // Begin is durably flushed BEFORE AddFile or shared metadata creation.
            // A crash with an open Begin fails closed rather than silently reusing
            // an incompletely staged media operation.
            journal.MediaStaging.Begin(e.OperationId,e.GameId);
            try
            {
                if((draft==null || draft.Cover) && MetadataPlan.Missing(game.CoverImage) && MetadataPlan.LooksLikeJpeg(cover))
                {
                    mediaUploadUncertain=true;
                    e.AfterCover=SaveImage(api,game.Id,cover);
                    imported.Add(e.AfterCover);
                    journal.MediaStaging.Track(e.OperationId,"cover",e.AfterCover);
                    mediaUploadUncertain=false;
                    e.ChangedCover=true;
                }
                if((draft==null || draft.Icon) && MetadataPlan.Missing(game.Icon) && MetadataPlan.LooksLikeJpeg(icon))
                {
                    mediaUploadUncertain=true;
                    e.AfterIcon=SaveImage(api,game.Id,icon);
                    imported.Add(e.AfterIcon);
                    journal.MediaStaging.Track(e.OperationId,"icon",e.AfterIcon);
                    mediaUploadUncertain=false;
                    e.ChangedIcon=true;
                }
                if((draft==null || draft.Background) && MetadataPlan.Missing(game.BackgroundImage) && MetadataPlan.LooksLikeJpeg(background))
                {
                    mediaUploadUncertain=true;
                    e.AfterBackground=SaveImage(api,game.Id,background);
                    imported.Add(e.AfterBackground);
                    journal.MediaStaging.Track(e.OperationId,"background",e.AfterBackground);
                    mediaUploadUncertain=false;
                    e.ChangedBackground=true;
                }
                // Only import reference metadata if the game did not already have it.
                // Playnite manages shared Genre/Company/Series objects by stable IDs.
                // Never use the catalog's generic first release year for a platform-specific
                // ROM, because a console game's GBA port may have a different release year.
                if((draft==null || draft.Release) && !game.ReleaseDate.HasValue && catalog.ReleaseDates!=null)
                {
                    var years=catalog.ReleaseDates.Where(r=>r!=null && r.PlatformId==platformId &&
                        r.Year>=1970 && r.Year<=DateTime.UtcNow.Year+5).Select(r=>r.Year).ToList();
                    if(years.Count>0)
                    {
                        e.AfterRelease=new ReleaseDate(years.Min()).Serialize();
                        e.ChangedRelease=true;
                    }
                }
                if((draft==null || draft.Genres) && (game.GenreIds==null || game.GenreIds.Count==0) && catalog.Genres!=null && catalog.Genres.Count>0)
                {
                    sharedReferenceWriteAttempted=true;
                    e.AfterGenres=catalog.Genres.Select(v=>api.Database.Genres.Add(v).Id).Distinct().ToList();
                    e.ChangedGenres=e.AfterGenres.Count>0;
                }
                if((draft==null || draft.Developers) && (game.DeveloperIds==null || game.DeveloperIds.Count==0) && catalog.Developers!=null && catalog.Developers.Count>0)
                {
                    sharedReferenceWriteAttempted=true;
                    e.AfterDevelopers=catalog.Developers.Select(v=>api.Database.Companies.Add(v).Id).Distinct().ToList();
                    e.ChangedDevelopers=e.AfterDevelopers.Count>0;
                }
                if((draft==null || draft.Publishers) && (game.PublisherIds==null || game.PublisherIds.Count==0) && catalog.Publishers!=null && catalog.Publishers.Count>0)
                {
                    sharedReferenceWriteAttempted=true;
                    e.AfterPublishers=catalog.Publishers.Select(v=>api.Database.Companies.Add(v).Id).Distinct().ToList();
                    e.ChangedPublishers=e.AfterPublishers.Count>0;
                }
                if((draft==null || draft.Series) && (game.SeriesIds==null || game.SeriesIds.Count==0) && catalog.SeriesNames!=null && catalog.SeriesNames.Count>0)
                {
                    sharedReferenceWriteAttempted=true;
                    e.AfterSeries=catalog.SeriesNames.Select(v=>api.Database.Series.Add(v).Id).Distinct().ToList();
                    e.ChangedSeries=e.AfterSeries.Count>0;
                }
                if(!EnrichmentPlan.HasChanges(e))
                {
                    // Reject an apparent no-op if ANY Playnite media or global
                    // entity write was attempted. Its uncertain effects must
                    // remain fail-closed, not be silently counted as skipped.
                    if(!EnrichmentPlan.CleanNoChange(e,imported.Count,
                        mediaUploadUncertain,sharedReferenceWriteAttempted))
                        throw new InvalidOperationException(
                            "Uncertain external staging during an otherwise empty import; recovery review required.");
                    noChangeAbandonAttempted=true;
                    journal.MediaStaging.AbandonClean(e.OperationId);
                    return CatalogApplyResult.Skipped();
                }
                journal.Prepare(e); prepared=true;
                journal.MediaStaging.HandOff(e.OperationId);
                if(e.ChangedName) game.Name=e.AfterName;
                if(e.ChangedSorting) game.SortingName=e.AfterSorting;
                if(e.ChangedDescription) game.Description=e.AfterDescription;
                if(e.ChangedCover) game.CoverImage=e.AfterCover;
                if(e.ChangedIcon) game.Icon=e.AfterIcon;
                if(e.ChangedBackground) game.BackgroundImage=e.AfterBackground;
                if(e.ChangedRelease) game.ReleaseDate=ReleaseDate.Deserialize(e.AfterRelease);
                if(e.ChangedGenres) game.GenreIds=e.AfterGenres.ToList();
                if(e.ChangedDevelopers) game.DeveloperIds=e.AfterDevelopers.ToList();
                if(e.ChangedPublishers) game.PublisherIds=e.AfterPublishers.ToList();
                if(e.ChangedSeries) game.SeriesIds=e.AfterSeries.ToList();
                api.Database.Games.Update(game);
                // Should a process crash between Update and Commit, undo still sees P and
                // can safely reconcile all before/after fields without guessing.
                journal.Commit(e.OperationId);
                return CatalogApplyResult.Applied(e);
            }
            catch
            {
                if(!prepared && !noChangeAbandonAttempted)
                {
                    bool cleanupSucceeded=true;
                    foreach(var id in imported)
                    {
                        try { if(!String.IsNullOrWhiteSpace(id)) api.Database.RemoveFile(id); }
                        catch { cleanupSucceeded=false; }
                    }
                    // Do not mark a failed/uncertain cleanup as complete. An open
                    // stage blocks future imports and must be inspected manually.
                    if(CatalogEnrichmentNeeds.MayAbandonCleanStage(cleanupSucceeded,
                        mediaUploadUncertain,sharedReferenceWriteAttempted))
                        journal.MediaStaging.AbandonClean(e.OperationId);
                    // A shared Genre/Company/Series Add() may have created a global
                    // record even when it throws before returning its ID. Leave the
                    // stage unresolved; manual recovery must not delete shared IDs.

                }
                throw;
            }
        }
        // User-directed Playnite display-name correction for an ALREADY journaled
        // catalog identity. This never edits ROM filenames, launch actions or media.
        // Unlike unattended enrichment it may permit a duplicate display name,
        // but only after a dedicated UI confirmation explicitly lists collisions.
        internal static EnrichmentEntry RenameDisplayTitle(IPlayniteAPI api, EnrichmentJournal journal,
            Guid gameId,string expectedName,long igdbId,string verifiedTitle,string approvedTitle,bool allowDuplicate)
        {
            if(api==null || journal==null || gameId==Guid.Empty || igdbId<=0 ||
               !EnrichmentPlan.IsSafeTitle(verifiedTitle))
                throw new InvalidOperationException("Invalid approved display-title operation.");
            var recovery=CatalogRecoveryConsistency.Inspect(
                journal.MediaStaging.AllStages(),journal.AllEntries());
            if(!recovery.AllowsWrites)
                throw new InvalidOperationException("An earlier operation needs recovery before renaming: " +
                    recovery.Problems[0]);
            // The identity must come from a committed import, never a heuristic-only scan.
            if(!journal.Committed().Any(x=>x.GameId==gameId && x.IgdbId==igdbId &&
                String.Equals(x.CatalogTitle,verifiedTitle,StringComparison.Ordinal)))
                throw new InvalidOperationException("No prior confirmed IGDB identity for this game.");
            var game=api.Database.Games[gameId];
            if(game==null || !EnrichmentPlan.IsExpected(game.Name,expectedName))
                throw new InvalidOperationException("Playnite title changed since review; reopen history.");
            var wanted=(approvedTitle??"").Trim();
            var simple=EnrichmentPlan.SuggestedDisplayTitle(game.Name,verifiedTitle);
            var suffix=EnrichmentPlan.PlatformDisplaySuffix(Platforms(game));
            var tagged=suffix.Length==0?"":simple+" ("+suffix+")";
            if(!EnrichmentPlan.IsSafeTitle(wanted) ||
               !(String.Equals(wanted,verifiedTitle.Trim(),StringComparison.Ordinal) ||
                 String.Equals(wanted,simple,StringComparison.Ordinal) ||
                 (suffix.Length>0 && String.Equals(wanted,tagged,StringComparison.Ordinal))))
                throw new InvalidOperationException("Display title does not match the approved catalog identity or safe cleanup.");
            if(String.Equals(game.Name,wanted,StringComparison.Ordinal))
                throw new InvalidOperationException("Playnite already has this display title.");
            var platforms=Platforms(game);
            var others=api.Database.Games.Where(g=>g!=null).Select(g=>
                Tuple.Create(g.Id,g.Name,Platforms(g))).ToList();
            bool collision=EnrichmentPlan.HasCollision(wanted,game.Id,platforms,others);
            if(collision && !allowDuplicate)
                throw new InvalidOperationException("Another same-platform game has this title; explicit approval required.");
            var e=new EnrichmentEntry {
                OperationId=Guid.NewGuid(),GameId=game.Id,IgdbId=igdbId,CatalogTitle=verifiedTitle,
                BeforeName=game.Name??"",AfterName=wanted,ChangedName=true,
                BeforeSorting=game.SortingName??"",AfterSorting=game.SortingName??"",
                Note=collision?"User approved duplicate Playnite display name; ROM files unchanged.":
                    "Playnite display title changed; ROM files unchanged."
            };
            if(!String.IsNullOrWhiteSpace(game.SortingName) &&
                EnrichmentPlan.MaySetSorting(game.SortingName,game.Name))
            {
                e.AfterSorting=wanted;e.ChangedSorting=true;
            }
            // Write-ahead journal protects the exact pre-change name and sorting.
            journal.Prepare(e);
            game.Name=e.AfterName;
            if(e.ChangedSorting) game.SortingName=e.AfterSorting;
            api.Database.Games.Update(game);
            journal.Commit(e.OperationId);
            return e;
        }
        internal static string Summary(EnrichmentEntry e)
        {
            var fields=new List<string>();
            if(e.ChangedName) fields.Add("name");
            if(e.ChangedDescription) fields.Add("description");
            if(e.ChangedCover) fields.Add("cover");
            if(e.ChangedIcon) fields.Add("icon");
            if(e.ChangedBackground) fields.Add("background");
            if(e.ChangedRelease) fields.Add("release year");
            if(e.ChangedGenres) fields.Add("genres");
            if(e.ChangedDevelopers) fields.Add("developers");
            if(e.ChangedPublishers) fields.Add("publishers");
            if(e.ChangedSeries) fields.Add("series");
            return "Updated "+String.Join(", ",fields)+". " +(e.Note??"")+" Undo available.";
        }
        internal static string UndoLast(IPlayniteAPI api,EnrichmentJournal journal)
        {
            var e=journal.LatestOpen();
            if(e==null) return "No enhanced metadata operation to undo.";
            var game=api.Database.Games[e.GameId];
            if(game==null) throw new InvalidOperationException("Original game was deleted; use Playnite's full backup.");
            if(!EnrichmentPlan.CanUndo(e,game.Name,game.SortingName,game.Description,game.CoverImage,
                game.Icon,game.BackgroundImage) ||
               !EnrichmentPlan.ExtraFieldsUndoSafe(e,GameExtrasSnapshot.ReleaseValue(game),
                    game.GenreIds,game.DeveloperIds,game.PublisherIds,game.SeriesIds))
                throw new InvalidOperationException("A field changed since import. Refusing to overwrite manual changes.");
            bool changed=false;
            if(e.ChangedName && EnrichmentPlan.IsExpected(game.Name,e.AfterName)) {game.Name=e.BeforeName;changed=true;}
            if(e.ChangedSorting && EnrichmentPlan.IsExpected(game.SortingName,e.AfterSorting)) {game.SortingName=e.BeforeSorting;changed=true;}
            if(e.ChangedDescription && EnrichmentPlan.IsExpected(game.Description,e.AfterDescription)) {game.Description=e.BeforeDescription;changed=true;}
            if(e.ChangedCover && EnrichmentPlan.IsExpected(game.CoverImage,e.AfterCover)) {game.CoverImage=e.BeforeCover;changed=true;}
            if(e.ChangedIcon && EnrichmentPlan.IsExpected(game.Icon,e.AfterIcon)) {game.Icon=e.BeforeIcon;changed=true;}
            if(e.ChangedBackground && EnrichmentPlan.IsExpected(game.BackgroundImage,e.AfterBackground)) {game.BackgroundImage=e.BeforeBackground;changed=true;}
            if(e.ChangedRelease && EnrichmentPlan.IsExpected(GameExtrasSnapshot.ReleaseValue(game),e.AfterRelease))
            { game.ReleaseDate=String.IsNullOrWhiteSpace(e.BeforeRelease)?(ReleaseDate?)null:ReleaseDate.Deserialize(e.BeforeRelease);changed=true; }
            if(e.ChangedGenres && EnrichmentPlan.SameIds(game.GenreIds,e.AfterGenres)) {game.GenreIds=(e.BeforeGenres??new List<Guid>()).ToList();changed=true;}
            if(e.ChangedDevelopers && EnrichmentPlan.SameIds(game.DeveloperIds,e.AfterDevelopers)) {game.DeveloperIds=(e.BeforeDevelopers??new List<Guid>()).ToList();changed=true;}
            if(e.ChangedPublishers && EnrichmentPlan.SameIds(game.PublisherIds,e.AfterPublishers)) {game.PublisherIds=(e.BeforePublishers??new List<Guid>()).ToList();changed=true;}
            if(e.ChangedSeries && EnrichmentPlan.SameIds(game.SeriesIds,e.AfterSeries)) {game.SeriesIds=(e.BeforeSeries??new List<Guid>()).ToList();changed=true;}
            if(changed) api.Database.Games.Update(game);
            journal.MarkUndone(e.OperationId);
            // Never delete user media. Only the newly imported IDs are considered;
            // if another game refers to an ID, the file is left in place.
            var created=new List<string>();
            if(e.ChangedCover) created.Add(e.AfterCover);
            if(e.ChangedIcon) created.Add(e.AfterIcon);
            if(e.ChangedBackground) created.Add(e.AfterBackground);
            int failures=0;
            foreach(var mediaId in created.Where(x=>!String.IsNullOrWhiteSpace(x)).Distinct())
            {
                bool shared=api.Database.Games.Any(g=>g!=null &&
                    (String.Equals(g.CoverImage,mediaId,StringComparison.Ordinal) ||
                     String.Equals(g.Icon,mediaId,StringComparison.Ordinal) ||
                     String.Equals(g.BackgroundImage,mediaId,StringComparison.Ordinal)));
                if(!shared) try { api.Database.RemoveFile(mediaId); } catch { failures++; }
            }
            return "Restored the previous name and metadata for "+e.BeforeName+
                (failures>0?". "+failures+" unused imported image(s) could not be cleaned up.":".");
        }
    }
}
