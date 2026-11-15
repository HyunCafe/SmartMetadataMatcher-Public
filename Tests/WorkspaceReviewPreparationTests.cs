using System;
using SmartMetadataMatcher;

internal static class WorkspaceReviewPreparationTests
{
    internal static void Run()
    {
        int checks=0;
        Action<bool,string> expect=(yes,name)=>{checks++;if(!yes)throw new Exception("Preparation: "+name);};
        expect(WorkspaceReviewPreparation.Next(0,0,false)==WorkspacePreparationStep.Review,"already prepared");
        expect(WorkspaceReviewPreparation.Next(1,0,false)==WorkspacePreparationStep.ScanPending,"one pending");
        expect(WorkspaceReviewPreparation.Next(0,1,false)==WorkspacePreparationStep.RefreshSaved,"one cached");
        expect(WorkspaceReviewPreparation.Next(2,3,false)==WorkspacePreparationStep.ScanPending,"mixed starts pending");
        expect(WorkspaceReviewPreparation.Next(0,3,false)==WorkspacePreparationStep.RefreshSaved,"mixed next cached");
        expect(WorkspaceReviewPreparation.Next(0,0,true)==WorkspacePreparationStep.Stop,"stop on empty");
        expect(WorkspaceReviewPreparation.Next(1,0,true)==WorkspacePreparationStep.Stop,"stop before scan");
        expect(WorkspaceReviewPreparation.Next(0,1,true)==WorkspacePreparationStep.Stop,"stop before refresh");
        expect(WorkspaceReviewPreparation.Next(1000,1000,true)==WorkspacePreparationStep.Stop,"stop even large mixed");
        expect(WorkspaceReviewPreparation.Next(1000,0,false)==WorkspacePreparationStep.ScanPending,"large pending");
        expect(WorkspaceReviewPreparation.Next(0,1000,false)==WorkspacePreparationStep.RefreshSaved,"large cached");
        foreach(var counts in new [] {new [] {-1,0},new [] {0,-1},new [] {-1,-1}})
        {
            bool rejected=false;
            try {WorkspaceReviewPreparation.Next(counts[0],counts[1],false);}
            catch(ArgumentOutOfRangeException){rejected=true;}
            expect(rejected,"negative counts rejected");
        }
        Console.WriteLine("PASS: "+checks+" pure Design C guided preparation routing checks.");
    }
}
