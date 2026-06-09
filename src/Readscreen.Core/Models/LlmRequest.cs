namespace Readscreen.Core.Models;

public record LlmRequest(
    string SystemPrompt,
    string UserPrompt,
    string Model,
    float Temperature = 0.3f);
