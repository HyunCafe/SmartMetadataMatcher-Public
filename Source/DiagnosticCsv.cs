using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartMetadataMatcher
{
    // No Playnite dependency: makes the export format testable without the host.
    internal sealed class DiagnosticRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
        public string Source { get; set; }
        public bool HasCover { get; set; }
        public bool HasBackground { get; set; }
        public bool HasDescription { get; set; }
        public bool HasIcon { get; set; }
        public bool HasReleaseDate { get; set; }
        public bool HasGenres { get; set; }
        public bool HasDevelopers { get; set; }
        public bool HasPublishers { get; set; }
        public bool IsInstalled { get; set; }
        public bool IsHidden { get; set; }
        public string MatcherStatus { get; set; }
        public string ProposedName { get; set; }
        public string MatcherReason { get; set; }

        public bool NeedsCoreMetadata { get { return !(HasCover && HasBackground && HasDescription); } }
        public bool BackgroundOnlyMissing { get { return HasCover && !HasBackground && HasDescription; } }
        public string MissingCoreFields
        {
            get
            {
                var missing = new List<string>();
                if (!HasCover) missing.Add("cover");
                if (!HasBackground) missing.Add("background");
                if (!HasDescription) missing.Add("description");
                return missing.Count == 0 ? "None" : String.Join(", ", missing);
            }
        }

        public string CoreStatus
        {
            get
            {
                int count = (HasCover ? 1 : 0) + (HasBackground ? 1 : 0) + (HasDescription ? 1 : 0);
                if (count == 0) return "Missing all core fields";
                if (count == 3) return "All core fields present";
                return "Partially missing core fields";
            }
        }
    }

    internal static class DiagnosticCsv
    {
        private static string YesNo(bool value) { return value ? "Yes" : "No"; }
        private static string Cell(string value)
        {
            // Keep one physical line per game. Always quote CSV cells (RFC 4180).
            var safe = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            // Prevent spreadsheet programs from interpreting imported text as formulas.
            var leading = safe.TrimStart();
            if (leading.Length > 0 && "=+-@".IndexOf(leading[0]) >= 0)
                safe = "'" + safe;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }
        internal static string Serialize(IEnumerable<DiagnosticRecord> records)
        {
            var columns = new[] {
                "Game Name", "Platform", "Source", "Core Metadata Status",
                "Missing Cover", "Missing Background", "Missing Description", "Missing Icon",
                "Missing Release Date", "Missing Genres", "Missing Developers", "Missing Publishers",
                "Matcher Status", "Suggested Name", "Matcher Reason", "Playnite Game ID",
                "Installed", "Hidden"
            };
            var sb = new StringBuilder();
            sb.AppendLine(String.Join(",", columns.Select(Cell)));
            foreach (var r in records)
            {
                var values = new[] {
                    r.Name, r.Platform, r.Source, r.CoreStatus,
                    YesNo(!r.HasCover), YesNo(!r.HasBackground), YesNo(!r.HasDescription),
                    YesNo(!r.HasIcon), YesNo(!r.HasReleaseDate), YesNo(!r.HasGenres),
                    YesNo(!r.HasDevelopers), YesNo(!r.HasPublishers),
                    r.MatcherStatus, r.ProposedName, r.MatcherReason, r.Id.ToString("D"),
                    YesNo(r.IsInstalled), YesNo(r.IsHidden)
                };
                sb.AppendLine(String.Join(",", values.Select(Cell)));
            }
            return sb.ToString();
        }
    }
}
