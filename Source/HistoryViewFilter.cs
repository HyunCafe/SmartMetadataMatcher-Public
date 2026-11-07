using System;

namespace SmartMetadataMatcher
{
    // Read-only history presentation. No journal entries are changed, hidden,
    // restored or made undo-eligible by filtering.
    internal static class HistoryViewFilter
    {
        internal const string All = "All";
        internal const string Applied = "Applied";
        internal const string Undone = "Undone";

        internal static bool Includes(string result, string game, string fields,
            string when, string stateFilter, string searchText)
        {
            var state = String.IsNullOrWhiteSpace(stateFilter) ? All : stateFilter.Trim();
            if (state != All && state != Applied && state != Undone) return false;
            if (state != All && !String.Equals(result, state, StringComparison.Ordinal)) return false;
            var query = (searchText ?? "").Trim();
            if (query.Length == 0) return true;
            return Contains(game,query) || Contains(fields,query) || Contains(when,query);
        }

        private static bool Contains(string value, string query)
        {
            return !String.IsNullOrEmpty(value) &&
                value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
