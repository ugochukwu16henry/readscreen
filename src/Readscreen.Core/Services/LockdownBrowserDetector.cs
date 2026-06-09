namespace Readscreen.Core.Services;

public static class LockdownBrowserDetector
{
    private static readonly string[] Indicators =
    [
        "lockdown",
        "respondus",
        "secure browser"
    ];

    public static bool IsLikelyLockdownBrowser(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().ToLowerInvariant();
        return Indicators.Any(normalized.Contains);
    }
}