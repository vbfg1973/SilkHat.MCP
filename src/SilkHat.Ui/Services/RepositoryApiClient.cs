using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SilkHat.Ui.Models;

namespace SilkHat.Ui.Services;

public sealed class RepositoryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RepositoryApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 50;

    public RepositoryApiClient(HttpClient httpClient, ILogger<RepositoryApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PagedResultModel<RepositoryGroupModel>> GetRepositoryGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetFromJsonAsync<PagedResultModel<RepositoryGroupModel>>(
                   "api/repository-groups",
                   cancellationToken)
               ?? new PagedResultModel<RepositoryGroupModel>(new List<RepositoryGroupModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<RepositoryGroupModel> CreateRepositoryGroupAsync(CreateRepositoryGroupRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/repository-groups", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RepositoryGroupModel>(cancellationToken: cancellationToken))!;
    }

    public async Task<RepositoryGroupModel> UpdateRepositoryGroupAsync(Guid id, UpdateRepositoryGroupRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/repository-groups/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RepositoryGroupModel>(cancellationToken: cancellationToken))!;
    }

    public async Task<PagedResultModel<RepositoryConfigModel>> GetRepositoryConfigsAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetFromJsonAsync<PagedResultModel<RepositoryConfigModel>>(
                   "api/repositories",
                   cancellationToken)
               ?? new PagedResultModel<RepositoryConfigModel>(new List<RepositoryConfigModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<PagedResultModel<RepositoryConfigModel>> GetLoadedRepositoryConfigsAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetFromJsonAsync<PagedResultModel<RepositoryConfigModel>>(
                   "api/repositories/loaded",
                   cancellationToken)
               ?? new PagedResultModel<RepositoryConfigModel>(new List<RepositoryConfigModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<PagedResultModel<AvailableRepositoryModel>> GetAvailableRepositoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetFromJsonAsync<PagedResultModel<AvailableRepositoryModel>>(
                   "api/repositories/available",
                   cancellationToken)
               ?? new PagedResultModel<AvailableRepositoryModel>(new List<AvailableRepositoryModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<PagedResultModel<AvailableRepositorySolutionModel>> GetAvailableRepositorySolutionsAsync(
        string rootPath,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/available/solutions?path={Uri.EscapeDataString(rootPath)}";
        return await GetFromJsonAsync<PagedResultModel<AvailableRepositorySolutionModel>>(url, cancellationToken)
               ?? new PagedResultModel<AvailableRepositorySolutionModel>(new List<AvailableRepositorySolutionModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<RepositoryConfigModel> CreateRepositoryConfigAsync(CreateRepositoryConfigRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/repositories", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RepositoryConfigModel>(cancellationToken: cancellationToken))!;
    }

    public async Task<RepositoryConfigModel> UpdateRepositoryConfigAsync(Guid id, UpdateRepositoryConfigRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/repositories/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RepositoryConfigModel>(cancellationToken: cancellationToken))!;
    }

    public async Task<RepositoryGroupLoadResultModel> LoadRepositoryGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/repository-groups/{id}/load", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RepositoryGroupLoadResultModel>(cancellationToken: cancellationToken))!;
    }

    public async Task<PagedResultModel<CodeSolutionModel>> GetCodeSolutionsAsync(
        Guid repositoryId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/{repositoryId}/code/solutions";
        return await GetFromJsonAsync<PagedResultModel<CodeSolutionModel>>(url, cancellationToken)
               ?? new PagedResultModel<CodeSolutionModel>(new List<CodeSolutionModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<PagedResultModel<CodeProjectDto>> GetCodeProjectsAsync(
        Guid repositoryId,
        string solutionId,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(name)
            ? $"api/repositories/{repositoryId}/code/solutions/{solutionId}/projects"
            : $"api/repositories/{repositoryId}/code/solutions/{solutionId}/projects?name={Uri.EscapeDataString(name)}";

        return await GetFromJsonAsync<PagedResultModel<CodeProjectDto>>(url, cancellationToken)
               ?? new PagedResultModel<CodeProjectDto>(new List<CodeProjectDto>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<PagedResultModel<CodeTreeEntryModel>> GetCodeTreeAsync(
        Guid repositoryId,
        string solutionId,
        string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/{repositoryId}/code/solutions/{solutionId}/tree";
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            url += $"?parentId={Uri.EscapeDataString(parentId)}";
        }

        return await GetFromJsonAsync<PagedResultModel<CodeTreeEntryModel>>(url, cancellationToken)
               ?? new PagedResultModel<CodeTreeEntryModel>(new List<CodeTreeEntryModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<CodeFileContentModel> GetCodeFileAsync(
        Guid repositoryId,
        string solutionId,
        string displayPath,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/{repositoryId}/code/solutions/{solutionId}/files?path={Uri.EscapeDataString(displayPath)}";
        return await GetFromJsonAsync<CodeFileContentModel>(url, cancellationToken)
               ?? new CodeFileContentModel(displayPath, displayPath, string.Empty);
    }

    public async Task<PagedResultModel<GitTreeEntryModel>> GetGitTreeAsync(
        Guid repositoryId,
        string? name = null,
        GitTreeEntryType? type = null,
        DateTimeOffset? changedAfter = null,
        string? author = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query.Add($"name={Uri.EscapeDataString(name)}");
        }

        if (type.HasValue)
        {
            query.Add($"type={Uri.EscapeDataString(type.Value.ToString())}");
        }

        if (changedAfter.HasValue)
        {
            query.Add($"changedAfter={Uri.EscapeDataString(changedAfter.Value.ToString("o"))}");
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query.Add($"author={Uri.EscapeDataString(author)}");
        }

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        var url = $"api/repositories/{repositoryId}/git/tree{queryString}";

        return await GetFromJsonAsync<PagedResultModel<GitTreeEntryModel>>(url, cancellationToken)
               ?? new PagedResultModel<GitTreeEntryModel>(new List<GitTreeEntryModel>(), DefaultPageNumber, DefaultPageSize, 0);
    }

    public async Task<GitFileHistoryModel> GetGitFileHistoryAsync(
        Guid repositoryId,
        string path,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/{repositoryId}/git/files/{Uri.EscapeDataString(path)}/history";
        return (await GetFromJsonAsync<GitFileHistoryModel>(url, cancellationToken))!;
    }

    public async Task<GitCoChangeStatsModel> GetGitCoChangesAsync(
        Guid repositoryId,
        string path,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/{repositoryId}/git/files/{Uri.EscapeDataString(path)}/cochanges";
        return (await GetFromJsonAsync<GitCoChangeStatsModel>(url, cancellationToken))!;
    }

    public async Task<GitFileLastChangeModel> GetGitFileLastChangeAsync(
        Guid repositoryId,
        string path,
        bool includeDiff,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/repositories/{repositoryId}/git/files/{Uri.EscapeDataString(path)}/last-change?includeDiff={includeDiff.ToString().ToLowerInvariant()}";
        return (await GetFromJsonAsync<GitFileLastChangeModel>(url, cancellationToken))!;
    }

    public async IAsyncEnumerable<RepoEventModel> LoadRepositoryAsync(
        Guid id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/repositories/{id}/load");
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var evt in ReadNdjsonAsync<RepoEventModel>(stream, cancellationToken))
        {
            yield return evt;
        }
    }

    private static async IAsyncEnumerable<T> ReadNdjsonAsync<T>(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<T>(line, JsonOptions);
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    private async Task<T?> GetFromJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("GET {Url}", url);
            return await _httpClient.GetFromJsonAsync<T>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET {Url} failed", url);
            throw;
        }
    }
}
