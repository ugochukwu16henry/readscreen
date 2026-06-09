using Readscreen.Core.Models;

namespace Readscreen.Core.Services;

public static class PromptBuilder
{
    public static LlmRequest Build(
        string question,
        AnswerMode mode,
        string model,
        string? screenText,
        string? transcript,
        IReadOnlyList<MemoryEntry> memories,
        IReadOnlyList<DocumentChunk> documents)
    {
        var systemPrompt = mode switch
        {
            AnswerMode.DocumentOnly =>
                "You are a presentation assistant. Answer ONLY using the provided document excerpts. " +
                "If the answer is not in the documents, say you cannot find it in the uploaded materials.",
            AnswerMode.PersonalMemory =>
                "You are a personal career assistant. Answer using the provided personal background information. " +
                "Speak in first person as the user. Be authentic and specific.",
            AnswerMode.General =>
                "You are a helpful assistant. Answer concisely and clearly.",
            AnswerMode.Hybrid =>
                "You are a personal assistant. Prefer document excerpts when relevant, then personal background, " +
                "then general knowledge. Be concise.",
            _ => "You are a helpful assistant."
        };

        var contextParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(screenText))
            contextParts.Add($"[Screen content]\n{screenText.Trim()}");

        if (!string.IsNullOrWhiteSpace(transcript))
            contextParts.Add($"[Recent audio transcript]\n{transcript.Trim()}");

        if (documents.Count > 0 && mode is AnswerMode.DocumentOnly or AnswerMode.Hybrid)
        {
            var docText = string.Join("\n---\n", documents.Select(d => $"({d.SourceFile}) {d.Content}"));
            contextParts.Add($"[Document excerpts]\n{docText}");
        }

        if (memories.Count > 0 && mode is AnswerMode.PersonalMemory or AnswerMode.Hybrid)
        {
            var memText = string.Join("\n---\n", memories.Select(m => $"[{m.Category}] {m.Title}: {m.Content}"));
            contextParts.Add($"[Personal background]\n{memText}");
        }

        var userPrompt = contextParts.Count > 0
            ? $"{string.Join("\n\n", contextParts)}\n\n[Question]\n{question}\n\nProvide a concise, helpful answer."
            : $"[Question]\n{question}\n\nProvide a concise, helpful answer.";

        return new LlmRequest(systemPrompt, userPrompt, model, 0.3f);
    }

    public static bool LooksLikeQuestion(string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        if (trimmed.EndsWith('?'))
            return true;

        var starters = new[] { "what ", "how ", "why ", "when ", "where ", "who ", "can you ", "could you ", "tell me ", "explain " };
        var lower = trimmed.ToLowerInvariant();
        return starters.Any(s => lower.StartsWith(s, StringComparison.Ordinal));
    }
}
