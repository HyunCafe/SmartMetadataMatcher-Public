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
        internal static EnrichmentEntry ApplyOne(IPlayniteAPI api, EnrichmentJournal journal,Guid gameId,
            string expectedName,string expectedPlatform,string expectedDescription,string expectedCover,
            string expectedIcon,string expectedBackground,string expectedSorting,CatalogGame catalog,
            byte[] cover,byte[] icon,byte[] background,bool rename,GameExtrasSnapshot expectedExtras)
        {
            if(catalog==null || catalog.Id<=0) throw new InvalidOperationException("Missing verified catalog ID.");
            if(expectedExtras==null) throw new InvalidOperationException("Missing original metadata snapshot.");
            // Fail closed on damaged journal before modifying the library.
            var pending=journal.OpenEntries();
            if(pending.Any(x=>x.GameId==gameId && !x.Committed))
                throw new InvalidOperationException("A previous interrupted metadata operation requires undo/recovery first.");
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
            if(rename && EnrichmentPlan.IsSafeTitle(catalog.Name) &&
               !String.Equals(game.Name,catalog.Name.Trim(),StringComparison.Ordinal))
            {
                var others=api.Database.Games.Where(g=>g!=null).Select(g=>
                    Tuple.Create(g.Id,g.Name,Platforms(g))).ToList();
                if(EnrichmentPlan.HasCollision(catalog.Name,game.Id,expectedPlatform,others))
                    e.Note="Catalog title collides with another same-platform library entry; name left unchanged.";
                else
                {
                    e.AfterName=catalog.Name.Trim(); e.ChangedName=true;
                    if(!String.IsNullOrWhiteSpace(game.SortingName) &&
                        EnrichmentPlan.MaySetSorting(game.SortingName,game.Name))
                    {
                        e.AfterSorting=e.AfterName; e.ChangedSorting=true;
                    }
                }
            }
            if(MetadataPlan.Missing(game.Description) && !String.IsNullOrWhiteSpace(catalog.Summary))
            {
                e.AfterDescription=MetadataPlan.HtmlFromSummary(catalog.Summary);
                e.ChangedDescription=true;
            }
            // Every imported image is tracked so undo can remove only files created by us.
            var imported=new List<string>();
            bool prepared=false;
            try
            {
                if(MetadataPlan.Missing(game.CoverImage) && MetadataPlan.LooksLikeJpeg(cover))
                {
                    e.AfterCover=SaveImage(api,game.Id,cover);
                    imported.Add(e.AfterCover); e.ChangedCover=true;
                }
                if(MetadataPlan.Missing(game.Icon) && MetadataPlan.LooksLikeJpeg(icon))
                {
                    e.AfterIcon=SaveImage(api,game.Id,icon);
                    imported.Add(e.AfterIcon); e.ChangedIcon=true;
                }
                if(MetadataPlan.Missing(game.BackgroundImage) && MetadataPlan.LooksLikeJpeg(background))
                {
                    e.AfterBackground=SaveImage(api,game.Id,background);
                    imported.Add(e.AfterBackground); e.ChangedBackground=true;
                }
                // Only import reference metadata if the game did not already have it.
                // Playnite manages shared Genre/Company/Series objects by stable IDs.
                // Never use the catalog's generic first release year for a platform-specific
                // ROM, because a console game's GBA port may have a different release year.
                if(!game.ReleaseDate.HasValue && catalog.ReleaseDates!=null)
                {
                    var years=catalog.ReleaseDates.Where(r=>r!=null && r.PlatformId==platformId &&
                        r.Year>=1970 && r.Year<=DateTime.UtcNow.Year+5).Select(r=>r.Year).ToList();
                    if(years.Count>0)
                    {
                        e.AfterRelease=new ReleaseDate(years.Min()).Serialize();
                        e.ChangedRelease=true;
                    }
                }
                if((game.GenreIds==null || game.GenreIds.Count==0) && catalog.Genres!=null && catalog.Genres.Count>0)
                {
                    e.AfterGenres=catalog.Genres.Select(v=>api.Database.Genres.Add(v).Id).Distinct().ToList();
                    e.ChangedGenres=e.AfterGenres.Count>0;
                }
                if((game.DeveloperIds==null || game.DeveloperIds.Count==0) && catalog.Developers!=null && catalog.Developers.Count>0)
                {
                    e.AfterDevelopers=catalog.Developers.Select(v=>api.Database.Companies.Add(v).Id).Distinct().ToList();
                    e.ChangedDevelopers=e.AfterDevelopers.Count>0;
                }
                if((game.PublisherIds==null || game.PublisherIds.Count==0) && catalog.Publishers!=null && catalog.Publishers.Count>0)
                {
                    e.AfterPublishers=catalog.Publishers.Select(v=>api.Database.Companies.Add(v).Id).Distinct().ToList();
                    e.ChangedPublishers=e.AfterPublishers.Count>0;
                }
                if((game.SeriesIds==null || game.SeriesIds.Count==0) && catalog.SeriesNames!=null && catalog.SeriesNames.Count>0)
                {
                    e.AfterSeries=catalog.SeriesNames.Select(v=>api.Database.Series.Add(v).Id).Distinct().ToList();
                    e.ChangedSeries=e.AfterSeries.Count>0;
                }
                if(!(e.ChangedName || e.ChangedDescription || e.ChangedCover ||
                     e.ChangedIcon || e.ChangedBackground || e.ChangedRelease ||
                     e.ChangedGenres || e.ChangedDevelopers || e.ChangedPublishers || e.ChangedSeries))
                    throw new InvalidOperationException("No safe missing fields or title change to apply. " + (e.Note??""));
                journal.Prepare(e); prepared=true;
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
                return e;
            }
            catch
            {
                if(!prepared)
                    foreach(var id in imported)
                        try { if(!String.IsNullOrWhiteSpace(id)) api.Database.RemoveFile(id); } catch { }
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
            if(EnrichmentPlan.HasUnresolvedJournalEntry(journal.OpenEntries()))
                throw new InvalidOperationException("An earlier operation needs recovery before renaming.");
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
