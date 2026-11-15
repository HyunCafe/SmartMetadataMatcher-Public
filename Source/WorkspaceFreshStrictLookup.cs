using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SmartMetadataMatcher
{
    // Provider-side read-only discovery. UI owns confirmation, cancellation and
    // session-only proof publication; this service owns all network requests.
    // Not a writer, not a persisted identity, and never import authorization.
    internal static class WorkspaceFreshStrictLookup
    {
        internal static void Check(
            IList<Tuple<CatalogInput,long,string,string>> requests,
            string clientId,string clientSecret,CancellationToken token,
            Action<Tuple<CatalogInput,long,string,string>> accepted,
            Action<int> progress)
        {
            if(requests==null || accepted==null || progress==null)
                throw new ArgumentNullException("Fresh strict verification inputs");
            if(String.IsNullOrWhiteSpace(clientId) || String.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("IGDB credentials are required.");
            int done=0;
            using(var client=new IgdbReadOnly(clientId,clientSecret))
            {
                foreach(var request in requests)
                {
                    token.ThrowIfCancellationRequested();
                    if(request==null || request.Item1==null ||
                       request.Item1.Id==Guid.Empty || request.Item2<=0 ||
                       String.IsNullOrWhiteSpace(request.Item3) ||
                       String.IsNullOrWhiteSpace(request.Item4))
                        throw new InvalidOperationException("Invalid fresh strict lookup snapshot.");
                    var input=request.Item1;
                    var variants=CatalogCore.QueryVariants(input.Name,input.Platform);
                    var matches=client.SearchVariants(variants.Select(x=>x.Text),token);
                    var resolved=CatalogWindow.ResolveWithExactFallback(input,matches,client,token);
                    if(CatalogCore.CanAutoPlan(resolved) && resolved.Game!=null &&
                       resolved.Game.Id==request.Item2 &&
                       String.Equals(resolved.Game.Name,request.Item3,StringComparison.Ordinal))
                        accepted(request);
                    done++;
                    progress(done);
                    if(done<requests.Count && token.WaitHandle.WaitOne(350))
                        token.ThrowIfCancellationRequested();
                }
            }
        }
    }
}
