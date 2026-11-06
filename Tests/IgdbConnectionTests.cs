using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartMetadataMatcher;

internal static class IgdbConnectionTests
{
    private static int checks;
    private static void Check(bool good,string name)
    { checks++;if(!good)throw new Exception("FAIL: IGDB connection "+name); }
    private sealed class FakeHandler : HttpMessageHandler
    {
        internal Queue<Func<HttpRequestMessage,HttpResponseMessage>> Steps =
            new Queue<Func<HttpRequestMessage,HttpResponseMessage>>();
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage m,CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if(Steps.Count==0)throw new Exception("Unexpected request");
            return Task.FromResult(Steps.Dequeue()(m));
        }
    }
    private static HttpResponseMessage Response(int code,string body)
    { return new HttpResponseMessage((HttpStatusCode)code) {Content=new StringContent(body)}; }
    private static FakeHandler Handler(int auth,int api,string authBody,string apiBody)
    {
        var h=new FakeHandler();
        h.Steps.Enqueue(m=>{
            Check(m.RequestUri.AbsoluteUri=="https://id.twitch.tv/oauth2/token","fixed OAuth HTTPS host");
            Check(m.Method==HttpMethod.Post,"OAuth POST");
            var body=m.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Check(body.Contains("client_id=sample") && body.Contains("client_secret=example"),"OAuth form credentials");
            return Response(auth,authBody);
        });
        if(auth>=200 && auth<300)
            h.Steps.Enqueue(m=>{
                Check(m.RequestUri.AbsoluteUri=="https://api.igdb.com/v4/games","fixed IGDB HTTPS host");
                Check(m.Method==HttpMethod.Post,"IGDB query POST");
                Check(m.Headers.Authorization!=null && m.Headers.Authorization.Parameter=="test-token","Bearer token");
                Check(m.Headers.Contains("Client-ID"),"IGDB Client-ID header");
                var text=m.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Check(text=="fields id; limit 1;","bounded read-only game query");
                Check(!text.Contains("example") && !text.Contains("sample"),"secrets absent from API body");
                return Response(api,apiBody);
            });
        return h;
    }
    private static bool Test(int auth,int api,string authBody,string apiBody,int expectedCalls=-1)
    {
        var h=Handler(auth,api,authBody,apiBody);
        using(var client=new IgdbReadOnly("sample","example",h))
        {
            var result=client.TestConnection(CancellationToken.None);
            Check(result!=null && !String.IsNullOrEmpty(result.Message),"result has safe message");
            Check(!result.Message.Contains("test-token") && !result.Message.Contains("example"),
                "result never leaks tokens or secrets");
            Check(h.Calls==(expectedCalls<0?(auth>=200 && auth<300?2:1):expectedCalls),"request count");
            return result.Connected;
        }
    }
    internal static void Run()
    {
        var token="{\"access_token\":\"test-token\"}";
        Check(Test(200,200,token,"[]"),"OAuth + IGDB both succeed (empty catalog valid)");
        Check(!Test(400,200,"{}","[]"),"Twitch bad credentials");
        Check(!Test(401,200,"{}","[]"),"Twitch unauthorized");
        Check(!Test(200,403,token,"[]"),"IGDB denied access");
        Check(!Test(200,429,token,"[]"),"IGDB rate limited");
        Check(!Test(503,200,"{}","[]"),"OAuth service unavailable");
        Check(!Test(200,200,token,"<html>bad</html>"),"malformed IGDB response");
        Check(!Test(200,200,"{}","[]",1),"missing OAuth access token");
        Check(IgdbConnectionResult.FromHttp(400,true).Message.Contains("Client ID"),"actionable OAuth message");
        Check(IgdbConnectionResult.FromHttp(429,false).Message.Contains("rate-limiting"),"rate-limit message");
        Check(IgdbConnectionResult.FromHttp(503,false).Message.Contains("temporarily"),"server problem message");
        Check(IgdbConnectionResult.Timeout().Message.Contains("timed out"),"timeout message");
        Check(IgdbConnectionResult.Network().Message.Contains("internet"),"network message");
        var cancel=new CancellationTokenSource();cancel.Cancel();
        var rejected=false;
        using(var client=new IgdbReadOnly("sample","example",Handler(200,200,token,"[]")))
        {
            try { client.TestConnection(cancel.Token); }
            catch(OperationCanceledException) {rejected=true;}
        }
        Check(rejected,"cancelled connection cannot become success");
        Console.WriteLine("PASS: "+checks+" offline mocked Twitch/IGDB connection checks; no network access.");
    }
}
