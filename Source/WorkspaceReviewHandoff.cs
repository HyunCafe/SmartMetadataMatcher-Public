using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Ephemeral list of Playnite game IDs selected in Design C. This is a UI
    // scope, not a catalog match, field plan, approval, or write authorization.
    // No persistence, network calls, or Playnite SDK dependencies.
    internal sealed class WorkspaceReviewHandoff
    {
        internal const int MaximumSelectedGames = 1000;
        // UI selection helpers use the same bound as the handoff contract.
        // Addition is all-or-nothing: never silently trim a user's review marks.
        internal static bool CanAddMarks(int existing, int additional)
        {
            return existing >= 0 && existing <= MaximumSelectedGames && additional >= 0 &&
                additional <= MaximumSelectedGames - existing;
        }
        private readonly HashSet<Guid> ids;

        internal WorkspaceReviewHandoff(IEnumerable<Guid> selected)
        {
            if (selected == null) throw new ArgumentNullException("selected");
            var values = selected.ToList();
            if (values.Count == 0 || values.Count > MaximumSelectedGames ||
                values.Any(id => id == Guid.Empty) || values.Distinct().Count() != values.Count)
                throw new ArgumentException("Choose 1 to 1000 different Playnite games for review.");
            ids = new HashSet<Guid>(values);
        }
        // Reviewing one focused game does not silently include the other marked
        // games. Neither scope is import approval; both use the same final plan.
        internal static WorkspaceReviewHandoff ForReviewScope(IEnumerable<Guid> marked,
            Guid? focusedGameId)
        {
            return focusedGameId.HasValue ?
                new WorkspaceReviewHandoff(new[] { focusedGameId.Value }) :
                new WorkspaceReviewHandoff(marked);
        }
        internal int Count { get { return ids.Count; } }
        internal bool Includes(Guid gameId) { return ids.Contains(gameId); }
        internal List<T> Filter<T>(IEnumerable<T> source, Func<T, Guid> gameId)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (gameId == null) throw new ArgumentNullException("gameId");
            return source.Where(item => ids.Contains(gameId(item))).ToList();
        }
        internal static bool CanMark(bool completed, bool ignored, bool fromHistory)
        {
            return !completed && !ignored && !fromHistory;
        }
    }
}
