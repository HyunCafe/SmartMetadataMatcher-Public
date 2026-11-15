using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Ephemeral view position only. No IGDB trust, approvals, credentials, paths,
    // or Playnite data changes. The live library decides which marks survive.
    internal sealed class WorkspaceReturnState
    {
        private readonly Guid[] marked;
        internal IEnumerable<Guid> MarkedGameIds { get { return marked; } }
        internal Guid FocusedGameId { get; private set; }
        internal int CategoryIndex { get; private set; }
        internal string SearchText { get; private set; }
        internal string Platform { get; private set; }
        internal int MissingFilterIndex { get; private set; }

        internal WorkspaceReturnState(IEnumerable<Guid> markedIds, Guid focusedGame,
            int categoryIndex, string searchText, string platform, int missingFilterIndex)
        {
            if(markedIds==null) throw new ArgumentNullException("markedIds");
            var list=markedIds.ToList();
            if(list.Count>WorkspaceReviewHandoff.MaximumSelectedGames ||
               list.Any(id=>id==Guid.Empty) || list.Distinct().Count()!=list.Count)
                throw new ArgumentException("Return state contains invalid or duplicate marked game IDs");
            if(categoryIndex<0 || categoryIndex>5 || missingFilterIndex<0 || missingFilterIndex>5)
                throw new ArgumentOutOfRangeException("Workspace view index is invalid");
            if((searchText??"").Length>1024 || (platform??"").Length>256)
                throw new ArgumentException("Workspace filter text is too long");
            marked=list.ToArray();
            FocusedGameId=focusedGame;
            CategoryIndex=categoryIndex;
            SearchText=searchText??"";
            Platform=String.IsNullOrWhiteSpace(platform)?"All platforms":platform;
            MissingFilterIndex=missingFilterIndex;
        }
        // A previous mark is not a new import authorization. Never revive an
        // applied, ignored, or missing game after returning from the review.
        internal bool KeepMark(Guid id, bool stillEligible)
        {
            return stillEligible && id!=Guid.Empty && marked.Contains(id);
        }
    }
}
