using System;
using System.Linq;

namespace SmartMetadataMatcher
{
    internal static class WorkspaceScanSelectionTests
    {
        internal static void Run()
        {
            var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
            var pending = new[] {a,b,c,a,Guid.Empty};
            Assert(WorkspaceScanSelection.Choose(pending, new Guid[0]).SequenceEqual(new[]{a,b,c}),
                "deduplicate pending rows");
            Assert(WorkspaceScanSelection.Choose(pending, new[]{c}).SequenceEqual(new[]{c}),
                "mark restricts selection, not import authority");
            Assert(WorkspaceScanSelection.Choose(pending, new[]{b,c},1).SequenceEqual(new[]{b}),
                "bound selection deterministically");
            Assert(WorkspaceScanSelection.Choose(pending, new[]{Guid.NewGuid()}).Count==0,
                "unknown mark cannot scan another game");
            Assert(WorkspaceScanSelection.Choose(new Guid[0],new Guid[0]).Count==0,
                "empty queue leaves cache unchanged");
            bool rejected=false;
            try {WorkspaceScanSelection.Choose(pending,new Guid[0],26);} catch(ArgumentOutOfRangeException){rejected=true;}
            Assert(rejected,"enforce 25-game limit");
            rejected=false;
            try {WorkspaceScanSelection.Choose(null,new Guid[0]);} catch(ArgumentNullException){rejected=true;}
            Assert(rejected,"fail closed on invalid queue");
            Console.WriteLine("PASS: 7 Design C native scan-selection policy checks.");
        }
        private static void Assert(bool yes,string detail)
        {
            if(!yes)throw new Exception("Design C scan selection failed: "+detail);
        }
    }
}
