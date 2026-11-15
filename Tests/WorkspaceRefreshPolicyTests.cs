using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    internal static class WorkspaceRefreshPolicyTests
    {
        internal static void Run()
        {
            var ids = Enumerable.Range(0, 29).Select(x => Guid.NewGuid()).ToList();
            var pick = WorkspaceRefreshPolicy.ChooseMarked(ids, ids);
            Check(pick.Count == WorkspaceScanSelection.BatchLimit, "bounded refresh");
            Check(pick.SequenceEqual(ids.Take(WorkspaceScanSelection.BatchLimit)), "stable order");
            Check(WorkspaceRefreshPolicy.ChooseMarked(ids, new[]{ids[2],ids[7]}).SequenceEqual(
                new[]{ids[2],ids[7]}), "marked scope only");
            Check(WorkspaceRefreshPolicy.ChooseMarked(new[]{ids[1],ids[1],Guid.Empty},
                new[]{ids[1],Guid.Empty}).SequenceEqual(new[]{ids[1]}), "deduplicate IDs");
            Check(WorkspaceRefreshPolicy.ChooseMarked(null, ids).Count == 0, "null cache scope");
            Check(WorkspaceRefreshPolicy.ChooseMarked(ids, null).Count == 0, "null mark scope");
            Check(WorkspaceRefreshPolicy.SameIdentity("123", "Some Game", "123", "Some Game"),
                "same exact ID and title");
            Check(!WorkspaceRefreshPolicy.SameIdentity("123", "Some Game", "124", "Some Game"),
                "changed ID unmarks");
            Check(!WorkspaceRefreshPolicy.SameIdentity("123", "Some Game", "123", "Some Game: Remastered"),
                "changed edition/title unmarks");
            Check(!WorkspaceRefreshPolicy.SameIdentity("", "Some Game", "123", "Some Game"),
                "unmatched old evidence never keeps mark");
            Check(!WorkspaceRefreshPolicy.SameIdentity("123", "Some Game", "", "Some Game"),
                "unmatched refreshed result unmarks");
            Check(!WorkspaceRefreshPolicy.SameIdentity("123", "Some Game", "123", "some game"),
                "case change requires renewed selection");
            Check(!WorkspaceRefreshPolicy.SameIdentity("-12", "Some Game", "-12", "Some Game"),
                "invalid negative IGDB ID");
            Console.WriteLine("PASS: 13 native workspace live refresh and identity-change policy checks.");
        }
        private static void Check(bool ok, string label)
        {
            if (!ok) throw new Exception("Workspace refresh policy: " + label);
        }
    }
}
