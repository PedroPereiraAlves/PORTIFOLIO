namespace PedroHub.Application.Common.Interfaces;

public sealed record GitHubStarredRepo(
    string Name,
    string HtmlUrl,
    string? Description,
    string? Language,
    int Stars,
    IReadOnlyList<string> Topics,
    string OwnerLogin);

public interface IGitHubStarredClient
{
    Task<IReadOnlyList<GitHubStarredRepo>> ObterReposAsync(CancellationToken cancellationToken = default);
}
