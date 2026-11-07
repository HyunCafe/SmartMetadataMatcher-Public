using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartMetadataMatcher;

internal static class CatalogCandidateEvidenceTests
{
    private static int checks;
    private static void Check(bool ok,string message)
    { checks++;if(!ok)throw new Exception("FAIL: candidate evidence: "+message); }
    private static CatalogGame G(long id,string name,long platform)
    {return new CatalogGame {Id=id,Name=name,PlatformIds=new List<long>{platform},
        Platforms=new List<string>{platform==24?"Nintendo Game Boy Advance":"PC (Windows)"},
        FirstReleaseYear=2003,Summary="Read-only mock catalog context"};}
    private sealed class FakeHandler : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Calls++;
            if(Calls==1)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content=new StringContent("{\"access_token\":\"private-test-token\"}") });
            Check(request.RequestUri.AbsoluteUri=="https://api.igdb.com/v4/games", "HTTPS catalog host");
            var query=request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Check(query.Contains("game_type,version_parent") && query.Contains("release_dates.platform.id"),
                "bounded detail query includes version and release evidence");
            Check(query.Contains("where id = (1198,1199)") && query.Contains("limit 12;"),
                "numeric-only bounded candidate lookup");
            Check(!query.Contains("private-test-token") && !query.Contains("example"),
                "no credentials included in request body");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content=new StringContent(
                "[{\"id\":1198,\"name\":\"Crash Bandicoot 2: N-Tranced\",\"game_type\":0,"+
                "\"version_parent\":100,\"platforms\":[{\"id\":24,\"name\":\"Game Boy Advance\"}],"+
                "\"release_dates\":[{\"platform\":{\"id\":24},\"date\":1041379200}]},"+
                "{\"id\":90001,\"name\":\"unrequested\"}]") });
        }
    }
    internal static void Run()
    {
        var input=new CatalogInput {Name="03 Crash Bandicoot 2",Platform="Nintendo Game Boy Advance"};
        var a=G(1198,"Crash Bandicoot 2: N-Tranced",24);
        var b=G(1199,"Crash Bandicoot 2: Fake Edition",6);
        var raw=new List<CatalogGame>{b,a,a};
        var ids=CatalogCandidateEvidence.SelectIds(input,raw,1198);
        Check(ids.Count==2 && ids[0]==1198 && ids[1]==1199,"selected and same-platform ordering; de-duplicate IDs");
        var inspect=G(1198,"Crash Bandicoot 2: N-Tranced",24);
        inspect.GameTypeId=0;
        inspect.VersionParentId=100;
        inspect.ReleaseDates=new List<CatalogRelease>{new CatalogRelease{PlatformId=24,Year=2003},
            new CatalogRelease{PlatformId=6,Year=2010}};
        var text=CatalogCandidateEvidence.Describe(input,raw,ids,new[]{inspect});
        Check(text.Contains("1/2 requested") && text.Contains("partial/unknown"),"missing response IDs treated as partial");
        Check(text.Contains("platform year 2003") && !text.Contains("2010"),"platform-specific release only");
        Check(text.Contains("version of #100") && text.Contains("type #0"),"version and type evidence preserved without interpretation");
        Check(text.Contains("not an automatic identity approval"),"no approval inferred from ID/score");
        var full=CatalogCandidateEvidence.Describe(input,new[]{a},new[]{1198L},new[]{inspect});
        Check(full.Contains("search coverage unverified"),"even one returned IGDB record does not prove exhaustiveness");
        var many=Enumerable.Range(1,60).Select(i=>G(i,"Limbo",6)).ToList();
        var limited=CatalogCandidateEvidence.SelectIds(
            new CatalogInput{Name="Limbo",Platform="PC (Windows)"},many,17);
        Check(limited.Count==CatalogCandidateEvidence.MaxIds && limited[0]==17 &&
            limited.Distinct().Count()==CatalogCandidateEvidence.MaxIds,"bounded 12-ID probe");
        var partial=CatalogCandidateEvidence.Describe(new CatalogInput{Name="Limbo",Platform="PC (Windows)"},
            many,limited,many.Take(12));
        Check(partial.Contains("partial/unknown"),"candidate truncation must remain visible");
        Check(CatalogCandidateEvidence.SelectIds(input,new CatalogGame[0],0).Count==0,
            "empty input no lookups");
        var handler=new FakeHandler();
        using(var client=new IgdbReadOnly("sample","example",handler))
        {
            var records=client.InspectCandidateIds(new long[]{1198,1199},CancellationToken.None);
            Check(handler.Calls==2 && records.Count==1 && records[0].Id==1198,
                "only explicitly requested IDs returned; missing ID remains unresolved");
            Check(records[0].GameTypeId==0 && records[0].VersionParentId==100 &&
                records[0].ReleaseDates.Single().Year==2003,
                "read-only nested game type/version/platform release parsed");
            var repeated=client.InspectCandidateIds(new long[]{1198,1199},CancellationToken.None);
            Check(handler.Calls==2 && repeated.Count==1,"in-memory probe cache avoids repeated API calls");
            bool rejected=false;
            try{client.InspectCandidateIds(Enumerable.Range(1,13).Select(x=>(long)x),CancellationToken.None);}
            catch(ArgumentException){rejected=true;}
            Check(rejected && handler.Calls==2,"oversized probe fails before network request");
        }
        Console.WriteLine("PASS: "+checks+" bounded read-only rival evidence checks; mocked HTTP, no network or Playnite data.");
    }
}
