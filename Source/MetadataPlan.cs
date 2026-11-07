using System;
using System.Net;
using System.Text.RegularExpressions;

namespace SmartMetadataMatcher
{
    // Pure validation and formatting. No Playnite SDK, filesystem, or network calls.
    internal static class MetadataPlan
    {
        internal static string HtmlFromSummary(string summary)
        {
            if (String.IsNullOrWhiteSpace(summary)) return "";
            var text = summary.Trim().Replace("\r\n", "\n").Replace('\r', '\n');
            return "<p>" + WebUtility.HtmlEncode(text).Replace("\n", "<br />") + "</p>";
        }
        internal static bool Missing(string existing) { return String.IsNullOrWhiteSpace(existing); }
        internal static bool SafeImageId(string id)
        {
            return !String.IsNullOrWhiteSpace(id) &&
                Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{4,80}\z");
        }
        internal static bool LooksLikeJpeg(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 128 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 &&
                bytes[bytes.Length - 2] == 0xFF && bytes[bytes.Length - 1] == 0xD9;
        }
        internal static bool MatchesExpected(string actualName, string expectedName,
            string actualDescription, string expectedDescription,
            string actualCover, string expectedCover)
        {
            return String.Equals(actualName ?? "", expectedName ?? "", StringComparison.Ordinal) &&
                String.Equals(actualDescription ?? "", expectedDescription ?? "", StringComparison.Ordinal) &&
                String.Equals(actualCover ?? "", expectedCover ?? "", StringComparison.Ordinal);
        }
        internal static bool CanUndo(MetadataEntry entry, string liveDescription, string liveCover)
        {
            if (entry == null) return false;
            return (!entry.ChangedDescription ||
                    String.Equals(liveDescription ?? "", entry.AfterDescription ?? "", StringComparison.Ordinal)) &&
                   (!entry.ChangedCover ||
                    String.Equals(liveCover ?? "", entry.AfterCover ?? "", StringComparison.Ordinal));
        }
        internal static bool IsBefore(MetadataEntry entry, string liveDescription, string liveCover)
        {
            if (entry == null) return false;
            return (!entry.ChangedDescription ||
                    String.Equals(liveDescription ?? "", entry.BeforeDescription ?? "", StringComparison.Ordinal)) &&
                   (!entry.ChangedCover ||
                    String.Equals(liveCover ?? "", entry.BeforeCover ?? "", StringComparison.Ordinal));
        }
    }
}
