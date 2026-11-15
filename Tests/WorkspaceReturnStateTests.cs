using System;
using System.Collections.Generic;
using System.Linq;
using SmartMetadataMatcher;

internal static class WorkspaceReturnStateTests
{
    internal static void Run()
    {
        int checks=0;
        Action<bool,string> expect=(yes,label)=>{
            checks++;
            if(!yes)throw new Exception("Workspace return: "+label);
        };
        var first=Guid.NewGuid();var second=Guid.NewGuid();
        var previous=new WorkspaceReturnState(new[]{first,second},second,2,"puzzle",
            "Nintendo Game Boy Advance",1);
        expect(previous.MarkedGameIds.SequenceEqual(new[]{first,second}),"keeps ID order");
        expect(previous.FocusedGameId==second,"keeps focus");
        expect(previous.CategoryIndex==2,"keeps tab");
        expect(previous.SearchText=="puzzle","keeps search");
        expect(previous.Platform=="Nintendo Game Boy Advance","keeps platform");
        expect(previous.MissingFilterIndex==1,"keeps field filter");
        expect(previous.KeepMark(first,true),"keeps eligible game");
        expect(!previous.KeepMark(first,false),"drops completed or ignored game");
        expect(!previous.KeepMark(Guid.NewGuid(),true),"never adds unrelated game");
        expect(!previous.KeepMark(Guid.Empty,true),"rejects empty ID");
        expect(new WorkspaceReturnState(new Guid[0],Guid.Empty,0,null,null,0)
            .Platform=="All platforms","defaults platform");
        bool rejected=false;
        try{new WorkspaceReturnState(null,Guid.Empty,0,"","",0);}
        catch(ArgumentNullException){rejected=true;}
        expect(rejected,"null marks rejected");
        rejected=false;
        try{new WorkspaceReturnState(new[]{first,first},Guid.Empty,0,"","",0);}
        catch(ArgumentException){rejected=true;}
        expect(rejected,"duplicate marks rejected");
        rejected=false;
        try{new WorkspaceReturnState(new[]{Guid.Empty},Guid.Empty,0,"","",0);}
        catch(ArgumentException){rejected=true;}
        expect(rejected,"empty marks rejected");
        rejected=false;
        try{new WorkspaceReturnState(new Guid[0],Guid.Empty,6,"","",0);}
        catch(ArgumentOutOfRangeException){rejected=true;}
        expect(rejected,"unknown tab rejected");
        rejected=false;
        try{new WorkspaceReturnState(new Guid[0],Guid.Empty,0,"","",6);}
        catch(ArgumentOutOfRangeException){rejected=true;}
        expect(rejected,"unknown field filter rejected");
        expect(new WorkspaceReturnState(new Guid[0],Guid.Empty,0,"","",4).MissingFilterIndex==4,
            "meaningful-gap filter survives handoff");
        expect(new WorkspaceReturnState(new Guid[0],Guid.Empty,0,"","",5).MissingFilterIndex==5,
            "Series-only filter survives handoff");
        rejected=false;
        try{new WorkspaceReturnState(new Guid[0],Guid.Empty,0,new string('x',1025),"",0);}
        catch(ArgumentException){rejected=true;}
        expect(rejected,"unbounded search rejected");
        rejected=false;
        try{new WorkspaceReturnState(Enumerable.Range(0,
            WorkspaceReviewHandoff.MaximumSelectedGames+1).Select(x=>Guid.NewGuid()),
            Guid.Empty,0,"","",0);}
        catch(ArgumentException){rejected=true;}
        expect(rejected,"oversized marked set rejected");
        Console.WriteLine("PASS: "+checks+" workspace return and safety checks.");
    }
}
