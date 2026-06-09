using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Llm;

public sealed class OllamaClient : ILlmClient
{
    private readonly IAppSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(IAppSettings settings, HttpClient httpClient, ILogger<OllamaClient> logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
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

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        LlmRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.Current.OllamaBaseUrl.TrimEnd('/')}/api/generate";
        var payload = new
        {
            model = request.Model,
            prompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}",
            stream = true,
            options = new { temperature = request.Temperature }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Ollama error: {Error}", error);
            yield return "[LLM unavailable]";
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            JsonElement json;
            try
            {
                json = JsonSerializer.Deserialize<JsonElement>(line);
            }
            catch
            {
                continue;
            }

            if (json.TryGetProperty("response", out var token))
            {
                var text = token.GetString();
                if (!string.IsNullOrEmpty(text))
                    yield return text;
            }

            if (json.TryGetProperty("done", out var done) && done.GetBoolean())
                break;
        }
    }
}
