using System.Net;
using PedroHub.Application.Common.Interfaces;

namespace PedroHub.Infrastructure.Monitoramento;

public sealed class SystemDnsResolver : IDnsResolver
{
    public Task<IPAddress[]> ResolverAsync(string host, CancellationToken cancellationToken = default) =>
        Dns.GetHostAddressesAsync(host, cancellationToken);
}
