using System;
using SmartMetadataMatcher;

internal static class WorkspaceTaskClassifierTests
{
    internal static void Run()
    {
        int checks=0;
        Action<WorkspaceTaskInput,WorkspaceTaskGroup,string> expected=(input,want,label)=>{
            var actual=WorkspaceTaskClassifier.Classify(input);
            checks++;
            if(actual.Group!=want)throw new Exception("Workspace state: "+label+
                " expected "+want+" got "+actual.Group);
        };
        Func<WorkspaceTaskInput> sample=()=>new WorkspaceTaskInput {
            Lookup=WorkspaceLookupState.Saved,
            Identity=WorkspaceIdentityState.Suggested,
            Fields=WorkspaceFieldsState.Potential,
            Write=WorkspaceWriteState.NotApplied,
            HasSupportedMetadataGaps=true
        };
        var s=sample();
        expected(s,WorkspaceTaskGroup.Suggestions,"saved candidate remains suggestion");
        checks++;
        if(!WorkspaceTaskClassifier.Classify(s).RequiresLivePreflight)
            throw new Exception("saved results need live preflight before applying");
        s.Identity=WorkspaceIdentityState.UserConfirmed;
        expected(s,WorkspaceTaskGroup.Suggestions,"confirmed identity is not completed import");
        s.Lookup=WorkspaceLookupState.Fresh;s.Identity=WorkspaceIdentityState.FreshStrict;
        expected(s,WorkspaceTaskGroup.Suggestions,"fresh strict remains a suggestion until approval");
        s.Fields=WorkspaceFieldsState.VerifiedAdditions;
        expected(s,WorkspaceTaskGroup.Suggestions,"verified additions still require approval");
        s.Fields=WorkspaceFieldsState.VerifiedNoChanges;
        expected(s,WorkspaceTaskGroup.NoNewInformation,"no-change requires actual field verification");
        checks++;
        if(WorkspaceTaskClassifier.Classify(s).RequiresLivePreflight)
            throw new Exception("no-change cannot appear as update-ready");
        s.Fields=WorkspaceFieldsState.Uninspected;
        expected(s,WorkspaceTaskGroup.Suggestions,"uninspected fields not falsely labeled nothing new");
        s.HasSupportedMetadataGaps=false;
        expected(s,WorkspaceTaskGroup.NoNewInformation,"no supported gaps");
        s=sample();s.Identity=WorkspaceIdentityState.NeedsReview;
        expected(s,WorkspaceTaskGroup.NeedsChoice,"ambiguous edition");
        s=sample();s.Identity=WorkspaceIdentityState.Unmatched;
        expected(s,WorkspaceTaskGroup.NotFound,"no IGDB identity");
        s=sample();s.Identity=WorkspaceIdentityState.Unknown;
        expected(s,WorkspaceTaskGroup.NeedsChoice,"missing identity");
        s=sample();s.Fields=WorkspaceFieldsState.Conflict;
        expected(s,WorkspaceTaskGroup.NeedsChoice,"conflicting fields");
        s=sample();s.Lookup=WorkspaceLookupState.NotChecked;
        expected(s,WorkspaceTaskGroup.CheckNeeded,"newly imported Playnite game");
        s=sample();s.Lookup=WorkspaceLookupState.Failed;
        expected(s,WorkspaceTaskGroup.CouldNotCheck,"network error is not no-change");
        s=sample();s.Lookup=WorkspaceLookupState.NotChecked;s.HasSupportedMetadataGaps=false;
        expected(s,WorkspaceTaskGroup.NoNewInformation,"complete game does not need an IGDB lookup");
        s=sample();s.Lookup=WorkspaceLookupState.Failed;s.HasSupportedMetadataGaps=false;
        expected(s,WorkspaceTaskGroup.NoNewInformation,"no gaps has priority over irrelevant failed lookup");
        s=sample();s.Write=WorkspaceWriteState.Applied;
        expected(s,WorkspaceTaskGroup.Updated,"committed and verified import takes priority");
        s=sample();s.Write=WorkspaceWriteState.Ignored;
        expected(s,WorkspaceTaskGroup.Ignored,"ignored game excluded");
        s=sample();s.Write=WorkspaceWriteState.Failed;
        expected(s,WorkspaceTaskGroup.NeedsRecovery,"failed write remains visible");
        s=sample();s.Write=WorkspaceWriteState.RecoveryBlocked;
        expected(s,WorkspaceTaskGroup.NeedsRecovery,"inconsistent journal blocks ordinary flow");
        s=sample();s.Write=WorkspaceWriteState.Undone;
        expected(s,WorkspaceTaskGroup.Suggestions,"undone metadata not treated as applied");
        bool threw=false;
        try {WorkspaceTaskClassifier.Classify(null);}catch(ArgumentNullException){threw=true;}
        checks++;if(!threw)throw new Exception("null workspace input accepted");
        Console.WriteLine("PASS: "+checks+" pure read-only workspace task classification checks.");
    }
}
