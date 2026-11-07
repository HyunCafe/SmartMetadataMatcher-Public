using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartMetadataMatcher
{
    // Pure title-analysis code: no Playnite SDK, network requests, or library mutations.
    internal sealed class GameDescriptor
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
        public string Source { get; set; }
    }

    internal sealed class RenameProposal
    {
        public Guid Id { get; set; }
        public string Original { get; set; }
        public string Proposed { get; set; }
        public string Platform { get; set; }
        public string Source { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }

    internal static class NameRules
    {
        // Only two-digit prefixes. Do not treat 007, 1942, Rayman 3 or FF VII as indices.
        private static readonly Regex Index = new Regex(
            @"^(?<number>0[1-9]|[1-9][0-9])[\s._-]+(?<name>[\p{L}][^\r\n]{2,})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex TrailingArticle = new Regex(
            @"^(?<name>.+?),\s*(?<article>The|An|A)(?<suffix>\s*[-:]\s*.+)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex Spaces = new Regex(@"\s{2,}", RegexOptions.Compiled);
        private static readonly Regex PspSuffix = new Regex(@"^(?<name>.+?)\s+PSP$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        // Curated search-title candidates only. An alias is NEVER sufficient proof of identity.
        private static readonly Dictionary<string, string> CandidateAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Cladun This Is An RPG", "ClaDun: This is an RPG" },
                { "Cladun X 2", "ClaDun x2" },
                { "Final Fantasy VII Crisis Core", "Crisis Core: Final Fantasy VII" }
            };

        private static string Safe(string s) { return s ?? ""; }
        private static string GroupKey(GameDescriptor game)
        {
            return Safe(game.Platform).Trim().ToLowerInvariant() + "\u001f" +
                   Safe(game.Source).Trim().ToLowerInvariant();
        }

        private static bool HasIndexSequence(IEnumerable<GameDescriptor> group)
        {
            // Three consecutive numbered items, not just three arbitrary two-digit titles.
            // Platform is required: grouping all unknown-platform PC games would be unsafe.
            var list = group.ToList();
            if (list.Count == 0 || String.IsNullOrWhiteSpace(list[0].Platform)) return false;
            var indexes = new HashSet<int>();
            foreach (var game in list)
            {
                var m = Index.Match(Safe(game.Name));
                if (m.Success) indexes.Add(Int32.Parse(m.Groups["number"].Value));
            }
            return indexes.Any(n => indexes.Contains(n + 1) && indexes.Contains(n + 2));
        }

        internal static List<RenameProposal> Analyze(IEnumerable<GameDescriptor> games)
        {
            var list = games.Where(g => g != null && !String.IsNullOrWhiteSpace(g.Name)).ToList();
            var sequences = list.GroupBy(GroupKey).ToDictionary(g => g.Key,
                g => HasIndexSequence(g), StringComparer.OrdinalIgnoreCase);
            var output = new List<RenameProposal>();

            foreach (var game in list)
            {
                var original = game.Name;
                var proposed = Spaces.Replace(original.Trim(), " ");
                var reasons = new List<string>();
                bool needsReview = false;
                if (proposed != original) reasons.Add("Whitespace normalized");

                var index = Index.Match(proposed);
                if (index.Success)
                {
                    proposed = index.Groups["name"].Value.Trim();
                    reasons.Add("Two-digit prefix: " + index.Groups["number"].Value);
                    if (!sequences[GroupKey(game)])
                    {
                        needsReview = true;
                        reasons.Add("No confirmed sequential collection on this platform/source");
                    }
                }

                var article = TrailingArticle.Match(proposed);
                if (article.Success)
                {
                    var rest = article.Groups["name"].Value.Trim();
                    var end = article.Groups["suffix"].Success ? article.Groups["suffix"].Value : "";
                    proposed = article.Groups["article"].Value + " " + rest + end;
                    reasons.Add("Trailing article moved to front");
                    needsReview = true; // Editorial rewrite; no catalog verification.
                }
                // PSP is a platform tag only when the platform actually identifies a PSP game.
                // Other platforms and genuinely meaningful in-title "Portable" remain unchanged.
                var psp = PspSuffix.Match(proposed);
                if (psp.Success && (String.Equals(game.Platform, "Sony PlayStation Portable",
                    StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(game.Platform, "PSP", StringComparison.OrdinalIgnoreCase)))
                {
                    proposed = psp.Groups["name"].Value.Trim();
                    reasons.Add("PSP suffix removed (platform corroborated; confirm title)");
                    needsReview = true;
                }

                string candidate;
                if (CandidateAliases.TryGetValue(proposed, out candidate))
                {
                    proposed = candidate;
                    reasons.Add("Known catalog-title candidate; identity not independently verified");
                    needsReview = true;
                }
                if (String.Equals(proposed, original, StringComparison.Ordinal)) continue;

                output.Add(new RenameProposal
                {
                    Id = game.Id,
                    Original = original,
                    Proposed = proposed,
                    Platform = Safe(game.Platform),
                    Source = Safe(game.Source),
                    Status = needsReview ? "Review" : "Ready",
                    Reason = String.Join("; ", reasons)
                });
            }

            // Indexed lookups avoid quadratic all-pairs scans on large libraries.
            Func<string, string, string, string> titleKey = (platform, source, name) =>
                Safe(platform).Trim().ToLowerInvariant() + "\u001f" +
                Safe(source).Trim().ToLowerInvariant() + "\u001f" +
                Safe(name).Trim().ToLowerInvariant();
            var proposedCounts = output.GroupBy(p => titleKey(p.Platform, p.Source, p.Proposed))
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            var existingCounts = list.GroupBy(g => titleKey(g.Platform, g.Source, g.Name))
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            foreach (var proposal in output)
            {
                var key = titleKey(proposal.Platform, proposal.Source, proposal.Proposed);
                int existingCount;
                existingCounts.TryGetValue(key, out existingCount);
                // If a whitespace-only normalization shares the current title,
                // exclude the current game from collision counts.
                if (String.Equals(proposal.Original.Trim(), proposal.Proposed,
                    StringComparison.OrdinalIgnoreCase)) existingCount--;
                if (proposedCounts[key] > 1 || existingCount > 0)
                {
                    proposal.Status = "Review";
                    proposal.Reason += "; Possible same-platform/source title collision";
                }
            }
            return output.OrderBy(p => p.Original, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
