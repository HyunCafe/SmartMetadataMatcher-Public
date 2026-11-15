using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class WorkspaceReviewHandoffTests
{
    internal static void Run()
    {
        int checks = 0;
        Action<bool, string> ok = (test, message) => { checks++; if (!test) throw new Exception("Handoff: " + message); };
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var handoff = new WorkspaceReviewHandoff(new [] { a, b });
        ok(WorkspaceReviewHandoff.CanAddMarks(0, 1000), "1000 marks in an empty selection");
        ok(WorkspaceReviewHandoff.CanAddMarks(999, 1), "exactly 1000 marks permitted");
        ok(WorkspaceReviewHandoff.CanAddMarks(1000, 0), "unmarking at capacity allowed");
        ok(!WorkspaceReviewHandoff.CanAddMarks(1000, 1), "1001st mark rejected");
        ok(!WorkspaceReviewHandoff.CanAddMarks(998, 3), "bulk overflow rejected atomically");
        ok(!WorkspaceReviewHandoff.CanAddMarks(-1, 1), "negative existing count rejected");
        ok(!WorkspaceReviewHandoff.CanAddMarks(1, -1), "negative addition rejected");
        ok(!WorkspaceReviewHandoff.CanAddMarks(1001, 0), "preexisting overflow rejected");
        ok(handoff.Count == 2, "exact selection count");
        ok(handoff.Includes(a) && handoff.Includes(b), "selected IDs visible");
        ok(!handoff.Includes(c) && !handoff.Includes(Guid.Empty), "unselected and empty IDs blocked");
        ok(handoff.Filter(new [] { a, c, b }, id => id).SequenceEqual(new [] { a, b }), "ordered only-selected projection");
        ok(handoff.Filter(new [] { c }, id => id).Count == 0, "no unrelated fallback");
        ok(WorkspaceReviewHandoff.CanMark(false, false, false), "unresolved match markable");
        ok(!WorkspaceReviewHandoff.CanMark(true, false, false), "completed blocked");
        ok(!WorkspaceReviewHandoff.CanMark(false, true, false), "ignored blocked");
        ok(!WorkspaceReviewHandoff.CanMark(false, false, true), "historical blocked");
        Action<Action, string> rejects = (action, name) => {
            bool threw = false;
            try { action(); } catch (ArgumentException) { threw = true; }
            ok(threw, name);
        };
        rejects(() => new WorkspaceReviewHandoff(null), "null refused");
        rejects(() => new WorkspaceReviewHandoff(new Guid[0]), "empty refused");
        rejects(() => new WorkspaceReviewHandoff(new[] { Guid.Empty }), "empty ID refused");
        rejects(() => new WorkspaceReviewHandoff(new[] { a, a }), "duplicate refused");
        rejects(() => new WorkspaceReviewHandoff(Enumerable.Repeat(a, 1001)), "oversized refused");
        var large = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        ok(new WorkspaceReviewHandoff(large).Count == 1000, "1000 unique supported");
        ok(handoff.Filter(new [] { a, b, c }, id => id).Count == 2, "external item excluded");
        var focused = WorkspaceReviewHandoff.ForReviewScope(new[] { a, b }, c);
        ok(focused.Count == 1 && focused.Includes(c) && !focused.Includes(a) &&
            !focused.Includes(b), "focused-only review excludes unrelated marks");
        ok(focused.Filter(new[] { a, b, c }, id => id).SequenceEqual(new[] { c }),
            "focused-only projection does not leak marked games");
        var marked = WorkspaceReviewHandoff.ForReviewScope(new[] { a, b }, null);
        ok(marked.Count == 2 && marked.Includes(a) && marked.Includes(b) &&
            !marked.Includes(c), "ordinary marked review keeps existing exact scope");
        rejects(() => WorkspaceReviewHandoff.ForReviewScope(new[] { a }, Guid.Empty),
            "empty focused ID rejected rather than falling back to marks");
        rejects(() => WorkspaceReviewHandoff.ForReviewScope(new Guid[0], null),
            "empty marked selection remains invalid");
        Console.WriteLine("PASS: " + checks + " read-only workspace review handoff checks.");
    }
}
