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
                    // Consecutive library numbers are a useful suggestion, not
                    // proof: a real numbered title may coexist with a ROM list.
                    needsReview = true;
                    reasons.Add(sequences[GroupKey(game)]
                        ? "Sequential collection detected; confirm this number is not part of the title"
                        : "No sequential collection on this platform/source");
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
                // A matching platform tag is a reviewable search/display hint
                // on every mapped platform, never a title-specific rewrite.
                var withoutTag=CatalogCore.DiscoveryTitle(proposed,game.Platform);
                if (!String.Equals(withoutTag,proposed,StringComparison.Ordinal))
                {
                    proposed=withoutTag;
                    reasons.Add("Possible list prefix or platform tag removed; confirm title");
                    needsReview=true;
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
