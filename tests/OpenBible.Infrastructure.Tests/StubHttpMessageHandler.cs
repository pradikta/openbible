using System.Net;

namespace OpenBible.Infrastructure.Tests;

internal sealed class StubHttpMessageHandler(HttpStatusCode status, byte[] body) : HttpMessageHandler
{
    public Uri? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequest = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(body) });
    }
}
