using System;

namespace SmartMetadataMatcher
{
    // Read-only preparation stages for a selected-only Design C handoff.
    // This policy authorizes neither a catalog identity nor any metadata write.
    internal enum WorkspacePreparationStep
    {
        Stop, ScanPending, RefreshSaved, Review
    }

    internal static class WorkspaceReviewPreparation
    {
        internal static WorkspacePreparationStep Next(int pending, int saved, bool stopped)
        {
            if(pending<0 || saved<0)
                throw new ArgumentOutOfRangeException("Negative selected-work counts");
            if(stopped)return WorkspacePreparationStep.Stop;
            if(pending>0)return WorkspacePreparationStep.ScanPending;
            if(saved>0)return WorkspacePreparationStep.RefreshSaved;
            return WorkspacePreparationStep.Review;
        }
    }
}
