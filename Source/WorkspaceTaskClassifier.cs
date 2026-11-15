using System;

namespace SmartMetadataMatcher
{
    // Design C: presentation-only state projection. Never authorizes an import,
    // creates a match, calls IGDB, changes Playnite records, or writes journals.
    internal enum WorkspaceLookupState { NotChecked, Saved, Fresh, Failed }
    internal enum WorkspaceIdentityState { Unknown, Unmatched, NeedsReview, Suggested, UserConfirmed, FreshStrict }
    internal enum WorkspaceFieldsState { Uninspected, Potential, VerifiedAdditions, VerifiedNoChanges, Conflict }
    internal enum WorkspaceWriteState { NotApplied, Applied, Ignored, Failed, Undone, RecoveryBlocked }
    internal enum WorkspaceTaskGroup
    {
        CheckNeeded, Suggestions, NeedsChoice, NotFound, NoNewInformation,
        Updated, Ignored, CouldNotCheck, NeedsRecovery
    }
    internal sealed class WorkspaceTaskInput
    {
        internal WorkspaceLookupState Lookup;
        internal WorkspaceIdentityState Identity;
        internal WorkspaceFieldsState Fields;
        internal WorkspaceWriteState Write;
        internal bool HasSupportedMetadataGaps;
    }
    internal sealed class WorkspaceTaskDecision
    {
        internal WorkspaceTaskGroup Group;
        internal string NextAction;
        internal bool RequiresLivePreflight;
        // Intentionally no CanWrite, IsApproved or AutoApply property. A queue
        // label is NOT an import authorization or a source of trusted evidence.
    }
    internal static class WorkspaceTaskClassifier
    {
        internal static WorkspaceTaskDecision Classify(WorkspaceTaskInput input)
        {
            if (input == null) throw new ArgumentNullException("input");
            WorkspaceTaskGroup group;
            string action;
            // Recovery and historical write state take precedence over the
            // appearance of any cached or fresh lookup result.
            if (input.Write == WorkspaceWriteState.RecoveryBlocked ||
                input.Write == WorkspaceWriteState.Failed)
            { group = WorkspaceTaskGroup.NeedsRecovery; action = "Review interrupted update"; }
            else if (input.Write == WorkspaceWriteState.Ignored)
            { group = WorkspaceTaskGroup.Ignored; action = "Ignored"; }
            else if (input.Write == WorkspaceWriteState.Applied)
            { group = WorkspaceTaskGroup.Updated; action = "Updated"; }
            else if (!input.HasSupportedMetadataGaps)
            { group = WorkspaceTaskGroup.NoNewInformation; action = "No missing supported fields"; }
            else if (input.Lookup == WorkspaceLookupState.Failed)
            { group = WorkspaceTaskGroup.CouldNotCheck; action = "Retry lookup"; }
            else if (input.Lookup == WorkspaceLookupState.NotChecked)
            { group = WorkspaceTaskGroup.CheckNeeded; action = "Check game"; }
            else if (input.Fields == WorkspaceFieldsState.Conflict)
            { group = WorkspaceTaskGroup.NeedsChoice; action = "Review conflicting fields"; }
            else if (input.Identity == WorkspaceIdentityState.NeedsReview)
            { group = WorkspaceTaskGroup.NeedsChoice; action = "Choose correct game"; }
            else if (input.Identity == WorkspaceIdentityState.Unmatched)
            { group = WorkspaceTaskGroup.NotFound; action = "Search for correct game"; }
            else if (input.Fields == WorkspaceFieldsState.VerifiedNoChanges)
            { group = WorkspaceTaskGroup.NoNewInformation; action = "Nothing new to add"; }
            else if (input.Identity == WorkspaceIdentityState.Unknown)
            { group = WorkspaceTaskGroup.NeedsChoice; action = "Identify this game"; }
            else
            { group = WorkspaceTaskGroup.Suggestions; action = "Review suggested improvements"; }
            return new WorkspaceTaskDecision {
                Group = group,
                NextAction = action,
                RequiresLivePreflight = group == WorkspaceTaskGroup.Suggestions ||
                    group == WorkspaceTaskGroup.NeedsChoice
            };
        }
    }
}
