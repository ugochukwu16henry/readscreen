using Microsoft.Extensions.Logging;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.Core.Services;

public sealed class ContextOrchestrator
{
    private readonly IAppSettings _settings;
    private readonly ILlmClient _llmClient;
    private readonly IMemoryStore _memoryStore;
    private readonly IDocumentStore _documentStore;
    private readonly IOverlayService _overlay;
    private readonly ILogger<ContextOrchestrator> _logger;
    private readonly ChangeDetector _changeDetector = new();
    private readonly object _gate = new();

    private string _latestScreenText = string.Empty;
    private readonly Queue<string> _transcriptWindow = new();
    private const int MaxTranscriptLines = 20;
    private DateTime _lastQuestionAt = DateTime.MinValue;
    private CancellationTokenSource? _currentGeneration;

    public bool IsPaused { get; set; }
    public AssistantStatus Status { get; private set; } = AssistantStatus.Idle;

    public ContextOrchestrator(
        IAppSettings settings,
        ILlmClient llmClient,
        IMemoryStore memoryStore,
        IDocumentStore documentStore,
        IOverlayService overlay,
        ILogger<ContextOrchestrator> logger)
    {
        _settings = settings;
        _llmClient = llmClient;
        _memoryStore = memoryStore;
        _documentStore = documentStore;
        _overlay = overlay;
        _logger = logger;
    }

    public void OnScreenText(string text)
    {
        if (IsPaused || string.IsNullOrWhiteSpace(text))
            return;

        _latestScreenText = text;
        if (!_changeDetector.HasMeaningfulChange(text, _settings.Current.DebounceSeconds))
            return;

        if (!PromptBuilder.LooksLikeQuestion(text) && text.Length < 30)
            return;

        _ = ProcessQuestionAsync(text, "screen");
    }

    public void OnTranscript(string transcript)
    {
        if (IsPaused || string.IsNullOrWhiteSpace(transcript))
            return;

        lock (_gate)
        {
            _transcriptWindow.Enqueue(transcript.Trim());
            while (_transcriptWindow.Count > MaxTranscriptLines)
                _transcriptWindow.Dequeue();
        }

        if (!PromptBuilder.LooksLikeQuestion(transcript))
            return;

        _ = ProcessQuestionAsync(transcript, "audio");
    }

    private async Task ProcessQuestionAsync(string question, string source)
    {
        if ((DateTime.UtcNow - _lastQuestionAt).TotalSeconds < _settings.Current.DebounceSeconds)
            return;

        lock (_gate)
        {
            if (_currentGeneration != null)
            {
                _currentGeneration.Cancel();
                _currentGeneration.Dispose();
            }
            _currentGeneration = new CancellationTokenSource();
        }

        var ct = _currentGeneration!.Token;
        _lastQuestionAt = DateTime.UtcNow;

        try
        {
            SetStatus(AssistantStatus.Thinking);
            _overlay.ClearText();
            _overlay.UpdateText($"Thinking...\n\nQ: {question}");

            var settings = _settings.Current;
            var memories = settings.AnswerMode is AnswerMode.PersonalMemory or AnswerMode.Hybrid
                ? await _memoryStore.SearchAsync(question, 5, ct)
                : Array.Empty<MemoryEntry>();

            var documents = settings.AnswerMode is AnswerMode.DocumentOnly or AnswerMode.Hybrid
                           && settings.ActiveDocumentSessionId.HasValue
                ? await _documentStore.SearchAsync(question, settings.ActiveDocumentSessionId.Value, 5, ct)
                : Array.Empty<DocumentChunk>();

            string transcript;
            lock (_gate)
                transcript = string.Join("\n", _transcriptWindow);

            var request = PromptBuilder.Build(
                question,
                settings.AnswerMode,
                settings.LlmModel,
                _latestScreenText,
                transcript,
                memories,
                documents);

            SetStatus(AssistantStatus.Answering);
            _overlay.ClearText();
            _overlay.UpdateText($"Q: {question}\n\n");

            await foreach (var token in _llmClient.StreamCompletionAsync(request, ct))
            {
                _overlay.AppendStreamingToken(token);
            }

            _changeDetector.MarkProcessed(question);
            SetStatus(AssistantStatus.Idle);
            _logger.LogInformation("Answered question from {Source}", source);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Generation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process question");
            SetStatus(AssistantStatus.Error);
            _overlay.UpdateText($"Error: {ex.Message}");
        }
    }

    private void SetStatus(AssistantStatus status)
    {
        Status = status;
        _overlay.SetStatus(status);
    }
}
