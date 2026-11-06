using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    internal static class DiagnosticExport
    {
        // Reads the actual Playnite library, not just the subset that NameRules flags.
        // Only file write is the CSV explicitly chosen in the Save File dialog.
        internal static void Run(IPlayniteAPI api)
        {
            var save = new SaveFileDialog {
                Title = "Export Playnite metadata diagnostics (read-only)",
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "Playnite-Metadata-Diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv"
            };
            if (save.ShowDialog() != true) return;

            // Snapshot entries before applying pure title analysis. Never update any game.
            var games = api.Database.Games.Where(g => g != null).ToList();
            var proposals = NameRules.Analyze(games
                .Where(g => !String.IsNullOrWhiteSpace(g.Name))
                .Select(g => new GameDescriptor {
                    Id = g.Id,
                    Name = g.Name,
                    Platform = g.Platforms == null ? "" : String.Join(", ", g.Platforms.Select(p => p.Name)),
                    Source = g.Source == null ? "" : g.Source.Name
                }).ToList())
                .ToDictionary(p => p.Id);

            var records = new List<DiagnosticRecord>();
            foreach (var game in games.OrderBy(g => g.Name ?? "", StringComparer.OrdinalIgnoreCase).ThenBy(g => g.Id))
            {
                RenameProposal proposal;
                proposals.TryGetValue(game.Id, out proposal);
                records.Add(new DiagnosticRecord {
                    Id = game.Id,
                    Name = game.Name ?? "",
                    Platform = game.Platforms == null ? "" : String.Join(", ", game.Platforms.Select(p => p.Name)),
                    Source = game.Source == null ? "" : game.Source.Name,
                    HasCover = !String.IsNullOrWhiteSpace(game.CoverImage),
                    HasBackground = !String.IsNullOrWhiteSpace(game.BackgroundImage),
                    HasDescription = !String.IsNullOrWhiteSpace(game.Description),
                    HasIcon = !String.IsNullOrWhiteSpace(game.Icon),
                    HasReleaseDate = game.ReleaseDate.HasValue,
                    HasGenres = game.GenreIds != null && game.GenreIds.Count > 0,
                    HasDevelopers = game.DeveloperIds != null && game.DeveloperIds.Count > 0,
                    HasPublishers = game.PublisherIds != null && game.PublisherIds.Count > 0,
                    IsInstalled = game.IsInstalled,
                    IsHidden = game.Hidden,
                    MatcherStatus = proposal == null ? "No suggestion" : proposal.Status,
                    ProposedName = proposal == null ? "" : proposal.Proposed,
                    MatcherReason = proposal == null ? "No existing title-cleanup rule proposed a change" : proposal.Reason
                });
            }
            var csv = DiagnosticCsv.Serialize(records);
            File.WriteAllText(save.FileName, csv, new UTF8Encoding(true)); // BOM for Excel Unicode names.
            int missingAll = records.Count(r => r.CoreStatus == "Missing all core fields");
            int partial = records.Count(r => r.CoreStatus == "Partially missing core fields");
            int suggested = records.Count(r => r.MatcherStatus != "No suggestion");
            api.Dialogs.ShowMessage(
                "Read-only CSV export complete.\n\n" +
                "All games: " + records.Count + "\n" +
                "Missing all cover/background/description: " + missingAll + "\n" +
                "Missing some cover/background/description: " + partial + "\n" +
                "Matcher suggestions: " + suggested + "\n\n" +
                "Saved to: " + save.FileName + "\n\n" +
                "A filled field means Playnite stores a value/reference, not that the content is correct. " +
                "No game records, ROM files, paths or launch settings were changed.",
                "Smart Metadata Matcher");
        }
    }
}
