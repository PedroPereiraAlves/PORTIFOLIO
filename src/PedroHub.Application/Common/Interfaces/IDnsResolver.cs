using System.Net;

namespace PedroHub.Application.Common.Interfaces;

public interface IDnsResolver
{
    Task<IPAddress[]> ResolverAsync(string host, CancellationToken cancellationToken = default);
}
