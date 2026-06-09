using Readscreen.Core.Services;

namespace Readscreen.Core.Tests;

public class LockdownBrowserDetectorTests
{
    [Theory]
    [InlineData("Respondus LockDown Browser")]
    [InlineData("lockdownbrowser.exe")]
    [InlineData("Secure Browser - Exam Mode")]
    public void IsLikelyLockdownBrowser_ReturnsTrue_ForKnownIndicators(string value)
    {
        Assert.True(LockdownBrowserDetector.IsLikelyLockdownBrowser(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Chrome")]
    [InlineData("Microsoft Edge")]
    public void IsLikelyLockdownBrowser_ReturnsFalse_ForRegularApplications(string value)
    {
        Assert.False(LockdownBrowserDetector.IsLikelyLockdownBrowser(value));
    }
}