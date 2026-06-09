namespace Readscreen.Core.Services;

public sealed class ChangeDetector
{
    private string _lastNormalized = string.Empty;
    private DateTime _lastProcessedAt = DateTime.MinValue;

    public bool HasMeaningfulChange(string text, int debounceSeconds = 30)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (normalized == _lastNormalized)
            return false;

        if ((DateTime.UtcNow - _lastProcessedAt).TotalSeconds < debounceSeconds && Similar(normalized, _lastNormalized))
            return false;

        return true;
    }

    public void MarkProcessed(string text)
    {
        _lastNormalized = Normalize(text);
        _lastProcessedAt = DateTime.UtcNow;
    }

    private static string Normalize(string text) =>
        string.Join(' ', text.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .ToLowerInvariant();

    private static bool Similar(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        var shorter = a.Length < b.Length ? a : b;
        var longer = a.Length < b.Length ? b : a;
        return longer.Contains(shorter, StringComparison.Ordinal) && shorter.Length > longer.Length * 0.7;
    }
}
