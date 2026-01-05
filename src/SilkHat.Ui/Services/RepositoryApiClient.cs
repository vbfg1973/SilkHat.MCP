using System.Net.Http.Json;
using SilkHat.Ui.Models;

namespace SilkHat.Ui.Services;

public sealed class RepositoryApiClient
{
    private readonly HttpClient _httpClient;

    public RepositoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RepositoryGroupModel>> GetRepositoryGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<RepositoryGroupModel>>(
                   "api/repository-groups",
                   cancellationToken)
               ?? new List<RepositoryGroupModel>();
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

    public async Task<IReadOnlyList<RepositoryConfigModel>> GetRepositoryConfigsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<RepositoryConfigModel>>(
                   "api/repositories",
                   cancellationToken)
               ?? new List<RepositoryConfigModel>();
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
}
