using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Readscreen.Core.Interfaces;

namespace Readscreen.Perception;

/// <summary>
/// Transcribes audio via Ollama's API when a whisper-compatible model is available.
/// Install: ollama pull whisper (or use faster-whisper sidecar as alternative).
/// </summary>
public sealed class OllamaAsrService : IAsrService
{
    private readonly IAppSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaAsrService(IAppSettings settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
        _model = "whisper";
    }

    public bool IsAvailable => true;

    public async Task<string> TranscribeAsync(byte[] pcm16Mono16kHz, CancellationToken cancellationToken = default)
    {
        if (pcm16Mono16kHz.Length == 0)
            return string.Empty;

        // Write WAV header + PCM for Ollama multipart upload
        var wav = WrapAsWav(pcm16Mono16kHz, 16000, 1);
        var base64 = Convert.ToBase64String(wav);

        var payload = new
        {
            model = _model,
            prompt = "Transcribe this audio accurately.",
            images = new[] { base64 },
            stream = false
        };

        try
        {
            var url = $"{_settings.Current.OllamaBaseUrl.TrimEnd('/')}/api/generate";
            using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return json.TryGetProperty("response", out var text)
                ? text.GetString()?.Trim() ?? string.Empty
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte[] WrapAsWav(byte[] pcm, int sampleRate, short channels)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        var byteRate = sampleRate * channels * 2;
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcm.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcm.Length);
        writer.Write(pcm);
        return ms.ToArray();
    }
}
