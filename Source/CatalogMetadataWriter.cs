using System;
using System.IO;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    // Playnite writes are deliberately UI-thread only and limited to the first
    // verified, user-confirmed game. No automatic title changes or field overwrites.
    internal static class CatalogMetadataWriter
    {
        internal static string ApplyOne(IPlayniteAPI api, MetadataJournal journal,
            Guid gameId, string expectedName, string expectedPlatform,
            string expectedDescription, string expectedCover,
            CatalogGame catalog, byte[] downloadedCover)
        {
            if (catalog == null || catalog.Id <= 0) throw new InvalidOperationException("No verified IGDB game ID.");
            var game = api.Database.Games[gameId];
            if (game == null) throw new InvalidOperationException("The selected Playnite game no longer exists.");
            if (!MetadataPlan.MatchesExpected(game.Name, expectedName, game.Description, expectedDescription,
                game.CoverImage, expectedCover))
                throw new InvalidOperationException("Game title or metadata changed since preview. Rescan before applying.");
            var currentPlatform = game.Platforms == null ? "" :
                String.Join(", ", game.Platforms.Select(p => p.Name));
            if (!String.Equals(currentPlatform, expectedPlatform ?? "", StringComparison.Ordinal))
                throw new InvalidOperationException("Game platform changed since catalog scan. Rescan before applying.");
            long platformId = CatalogCore.PlatformId(currentPlatform);
            if (platformId <= 0 || catalog.PlatformIds == null || !catalog.PlatformIds.Contains(platformId))
                throw new InvalidOperationException("Catalog platform ID is not corroborated. This game requires manual investigation.");

            bool fillDescription = MetadataPlan.Missing(game.Description) && !String.IsNullOrWhiteSpace(catalog.Summary);
            bool fillCover = MetadataPlan.Missing(game.CoverImage) && MetadataPlan.LooksLikeJpeg(downloadedCover);
            if (!fillDescription && !fillCover)
                throw new InvalidOperationException("No missing description or usable IGDB cover to apply.");
            string oldDescription = game.Description ?? "";
            string oldCover = game.CoverImage ?? "";
            string newDescription = fillDescription ? MetadataPlan.HtmlFromSummary(catalog.Summary) : oldDescription;
            string newCover = oldCover;
            string tempFile = null;
            bool prepared = false;
            try
            {
                if (fillCover)
                {
                    tempFile = Path.Combine(Path.GetTempPath(), "SMM-IGDB-" + Guid.NewGuid().ToString("N") + ".jpg");
                    File.WriteAllBytes(tempFile, downloadedCover);
                    newCover = api.Database.AddFile(tempFile, game.Id);
                    if (String.IsNullOrWhiteSpace(newCover))
                        throw new IOException("Playnite did not return an image file ID.");
                }
                var entry = new MetadataEntry {
                    OperationId=Guid.NewGuid(), GameId=game.Id, IgdbId=catalog.Id, GameName=game.Name,
                    BeforeDescription=oldDescription, AfterDescription=newDescription,
                    BeforeCover=oldCover, AfterCover=newCover,
                    ChangedDescription=fillDescription, ChangedCover=fillCover
                };
                // Durable intent is recorded BEFORE game metadata changes.
                journal.Prepare(entry);
                prepared = true;
                if (fillDescription) game.Description = newDescription;
                if (fillCover) game.CoverImage = newCover;
                api.Database.Games.Update(game);
                return "Applied missing " + (fillDescription ? "description" : "") +
                    (fillDescription && fillCover ? " and " : "") + (fillCover ? "cover" : "") +
                    " to " + game.Name + ". Undo is available in the catalog window.";
            }
            catch
            {
                // If durable intent was never recorded, image bytes are not referenced
                // by the game and can be discarded. If prepared, keep the journal so
                // an interrupted/partial update can be inspected and safely undone.
                if (!prepared && !String.Equals(newCover, oldCover, StringComparison.Ordinal) &&
                    !String.IsNullOrWhiteSpace(newCover))
                {
                    try { api.Database.RemoveFile(newCover); } catch { }
                }
                throw;
            }
            finally
            {
                if (!String.IsNullOrEmpty(tempFile))
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                }
            }
        }

        internal static string UndoLast(IPlayniteAPI api, MetadataJournal journal)
        {
            var entry = journal.LatestPending();
            if (entry == null) return "No metadata changes to undo.";
            var game = api.Database.Games[entry.GameId];
            if (game == null) throw new InvalidOperationException("Original game no longer exists. Restore the library backup if necessary.");
            bool before = MetadataPlan.IsBefore(entry, game.Description, game.CoverImage);
            if (!before && !MetadataPlan.CanUndo(entry, game.Description, game.CoverImage))
                throw new InvalidOperationException("A changed field was edited after import. Refusing to overwrite newer customizations.");
            if (!before)
            {
                if (entry.ChangedDescription) game.Description = entry.BeforeDescription;
                if (entry.ChangedCover) game.CoverImage = entry.BeforeCover;
                api.Database.Games.Update(game);
            }
            journal.MarkUndone(entry.OperationId);
            // The image was imported by this operation, and only the original
            // reference is restored. Never delete any pre-existing user artwork.
            bool shared = entry.ChangedCover && api.Database.Games.Any(g=>g.Id!=entry.GameId &&
                String.Equals(g.CoverImage,entry.AfterCover,StringComparison.Ordinal));
            if (entry.ChangedCover && !shared && !String.Equals(entry.AfterCover,entry.BeforeCover,StringComparison.Ordinal))
            {
                try { api.Database.RemoveFile(entry.AfterCover); }
                catch (Exception ex) { return "Metadata restored, but unused image cleanup failed: " + ex.Message; }
            }
            return before ? "No database update was found; the interrupted operation was cleared." :
                "Restored the previous description and cover for " + game.Name + ".";
        }
    }
}
