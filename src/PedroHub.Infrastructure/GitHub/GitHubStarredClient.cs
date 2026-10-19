using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PedroHub.Application.Common.Interfaces;

namespace PedroHub.Infrastructure.GitHub;

public sealed class GitHubStarredClient : IGitHubStarredClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubStarredClient> _logger;

    public GitHubStarredClient(
        HttpClient http,
        IConfiguration configuration,
        ILogger<GitHubStarredClient> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GitHubStarredRepo>> ObterReposAsync(
        CancellationToken cancellationToken = default)
    {
        var token = _configuration["HUB_GITHUB_TOKEN"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("HUB_GITHUB_TOKEN não configurado.");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/starred?per_page=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("PedroHub");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        // Nunca logar o token
        _logger.LogInformation("Consultando repositórios com star no GitHub.");

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "GitHub API retornou {StatusCode} ao listar starred.",
                (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<List<GitHubApiRepo>>(cancellationToken)
                      ?? [];

        return payload
            .Where(r => r.HtmlUrl is not null && r.Name is not null && r.Owner?.Login is not null)
            .Select(r => new GitHubStarredRepo(
                r.Name!,
                r.HtmlUrl!,
                r.Description,
                r.Language,
                r.StargazersCount,
                r.Topics ?? [],
                r.Owner!.Login!))
            .ToList();
    }

    private sealed class GitHubApiRepo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("topics")]
        public List<string>? Topics { get; set; }

        [JsonPropertyName("owner")]
        public GitHubApiOwner? Owner { get; set; }
    }

    private sealed class GitHubApiOwner
    {
        [JsonPropertyName("login")]
        public string? Login { get; set; }
    }
}
