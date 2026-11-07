using System;
using System.Collections.Generic;
using SmartMetadataMatcher;

internal static class CatalogReviewActionsTests
{
    private static int checks;
    private static void Check(bool yes,string label)
    {checks++;if(!yes)throw new Exception("FAIL: "+label);}
    internal static void Run()
    {
        Check(CatalogReviewActions.PlatformVerified("Nintendo Game Boy Advance",987,"Super Mario Advance",new[]{24L}),
            "exact structured GBA platform verified");
        Check(!CatalogReviewActions.PlatformVerified("Nintendo DS",987,"Super Mario Advance",new[]{24L}),
            "wrong console rejected");
        Check(!CatalogReviewActions.PlatformVerified("Unknown Console",987,"Super Mario Advance",new[]{24L}),
            "unknown console rejected");
        Check(!CatalogReviewActions.PlatformVerified("Nintendo Game Boy Advance",987,"Mario",new long[0]),
            "absent platform evidence rejected");
        Check(!CatalogReviewActions.PlatformVerified("Nintendo Game Boy Advance",0,"Mario",new[]{24L}),
            "invalid IGDB identity rejected");
        Check(!CatalogReviewActions.PlatformVerified("Nintendo Game Boy Advance",987,"",new[]{24L}),
            "missing IGDB title rejected");
        var id=Guid.NewGuid();
        Check(CatalogReviewActions.SameLiveIdentity(id,"Mario","GBA",id,"Mario","GBA"),
            "unchanged live game accepted");
        Check(!CatalogReviewActions.SameLiveIdentity(id,"Mario","GBA",id,"Mario 2","GBA"),
            "title change rejects old identity");
        Check(!CatalogReviewActions.SameLiveIdentity(id,"Mario","GBA",id,"Mario","Nintendo DS"),
            "platform change rejects old identity");
        Check(!CatalogReviewActions.SameLiveIdentity(id,"Mario","GBA",Guid.NewGuid(),"Mario","GBA"),
            "different library game ID rejected");
        Console.WriteLine("PASS: "+checks+" IGDB review context-menu guards. No Playnite database accessed.");
    }
}
