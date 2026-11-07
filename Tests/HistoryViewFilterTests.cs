using System;
using SmartMetadataMatcher;

internal static class HistoryViewFilterTests
{
    private static int checks;
    private static void Check(bool condition,string name)
    {
        checks++;
        if(!condition)throw new Exception("FAIL: history display filter: "+name);
    }
    internal static void Run()
    {
        const string game="Pokémon Mystery Dungeon: Explorers of Sky";
        const string fields="Cover, Genres, Background, Series";
        const string when="2026-11-06 16:49";
        Check(HistoryViewFilter.Includes("Applied",game,fields,when,"All",""),"All shows applied");
        Check(HistoryViewFilter.Includes("Undone",game,fields,when,"All",""),"All shows undone");
        Check(HistoryViewFilter.Includes("Applied",game,fields,when,"Applied",""),"Applied state");
        Check(!HistoryViewFilter.Includes("Undone",game,fields,when,"Applied",""),"Applied omits undone");
        Check(HistoryViewFilter.Includes("Undone",game,fields,when,"Undone",""),"Undone state");
        Check(!HistoryViewFilter.Includes("Applied",game,fields,when,"Undone",""),"Undone omits applied");
        Check(HistoryViewFilter.Includes("Applied",game,fields,when,"All","  mystery  "),"trimmed case-insensitive title");
        Check(HistoryViewFilter.Includes("Applied",game,fields,when,"All","GENRES"),"field name search");
        Check(HistoryViewFilter.Includes("Applied",game,fields,when,"All","16:49"),"date search");
        Check(!HistoryViewFilter.Includes("Applied",game,fields,when,"All","wrong title"),"non-matching query");
        Check(!HistoryViewFilter.Includes("Applied",game,fields,when,"Other",""),"unexpected state fails closed");
        Check(HistoryViewFilter.Includes("Undone",null,null,null,null,null),"empty legacy fields tolerated");
        Check(!HistoryViewFilter.Includes("Undone",null,null,null,"Applied","Sky"),"state guard before search");
        Check(HistoryViewFilter.Includes("Applied",game,fields,when," Applied ",""),"filter whitespace");
        Console.WriteLine("PASS: "+checks+" read-only history status/search checks. No Playnite database accessed.");
    }
}
