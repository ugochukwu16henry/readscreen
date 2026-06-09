using System.Net.Http.Json;
using System.Text.Json;
using Readscreen.Core.Interfaces;

namespace Readscreen.Memory;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly IAppSettings _settings;
    private readonly HttpClient _httpClient;

    public EmbeddingService(IAppSettings settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_settings.Current.OllamaBaseUrl.TrimEnd('/')}/api/tags";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.Current.OllamaBaseUrl.TrimEnd('/')}/api/embeddings";
        var payload = new { model = _settings.Current.EmbeddingModel, prompt = text };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        if (!json.TryGetProperty("embedding", out var arr))
            return Array.Empty<float>();

        return arr.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
    }
}
