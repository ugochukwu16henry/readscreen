using Readscreen.Core.Models;
using Readscreen.Core.Services;

namespace Readscreen.Core.Tests;

public class ChangeDetectorTests
{
    [Fact]
    public void HasMeaningfulChange_ReturnsFalse_WhenTextUnchanged()
    {
        var detector = new ChangeDetector();
        Assert.True(detector.HasMeaningfulChange("Hello world"));
        detector.MarkProcessed("Hello world");
        Assert.False(detector.HasMeaningfulChange("Hello world"));
    }

    [Fact]
    public void HasMeaningfulChange_ReturnsTrue_WhenTextChanges()
    {
        var detector = new ChangeDetector();
        Assert.True(detector.HasMeaningfulChange("What is Python?"));
        detector.MarkProcessed("What is Python?");
        Assert.True(detector.HasMeaningfulChange("What is JavaScript?"));
    }
}

public class PromptBuilderTests
{
    [Fact]
    public void LooksLikeQuestion_DetectsQuestionMark()
    {
        Assert.True(PromptBuilder.LooksLikeQuestion("What is the capital of France?"));
        Assert.False(PromptBuilder.LooksLikeQuestion("Paris"));
    }

    [Fact]
    public void Build_IncludesPersonalMemory()
    {
        var memories = new[]
        {
            new MemoryEntry(Guid.NewGuid(), "Education", "BYU-Idaho", "Studied software engineering", DateTime.UtcNow)
        };

        var request = PromptBuilder.Build(
            "Tell me about your education",
            AnswerMode.PersonalMemory,
            "phi3",
            null,
            null,
            memories,
            Array.Empty<DocumentChunk>());

        Assert.Contains("BYU-Idaho", request.UserPrompt);
        Assert.Contains("first person", request.SystemPrompt);
    }
}

public class VectorSearchTests
{
    [Fact]
    public void TopKIndices_ReturnsMostSimilar()
    {
        var vectors = new[]
        {
            new[] { 1f, 0f, 0f },
            new[] { 0f, 1f, 0f },
            new[] { 0.9f, 0.1f, 0f }
        };
        var query = new[] { 1f, 0f, 0f };

        var indices = Readscreen.Memory.VectorSearchService.TopKIndices(vectors, query, 2);
        Assert.Equal(0, indices[0]);
        Assert.Equal(2, indices[1]);
    }
}
