namespace Readscreen.Core.Models;

public class AppSettings
{
    public CaptureRegionSettings CaptureRegion { get; set; } = new();
    public double PollIntervalSeconds { get; set; } = 3.0;
    public string LlmModel { get; set; } = "phi3";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public AnswerMode AnswerMode { get; set; } = AnswerMode.Hybrid;
    public double OverlayOpacity { get; set; } = 0.85;
    public bool ClickThrough { get; set; }
    public bool AudioEnabled { get; set; } = true;
    public int AudioChunkSeconds { get; set; } = 8;
    public int DebounceSeconds { get; set; } = 30;
    public string DataDirectory { get; set; } = "";
    public Guid? ActiveDocumentSessionId { get; set; }
}

public class CaptureRegionSettings
{
    public int Top { get; set; } = 200;
    public int Left { get; set; } = 200;
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 400;
}
