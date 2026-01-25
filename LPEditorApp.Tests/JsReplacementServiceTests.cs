using LPEditorApp.Services;
using Xunit;

namespace LPEditorApp.Tests;

public class JsReplacementServiceTests
{
    [Fact]
    public void ReplaceCountdownEnd_ReplacesAllOccurrences()
    {
        var service = new JsReplacementService();
        var js = "const endDate = new Date('2026-01-01T23:59:59');\nvar endDate = new Date(\"2026-02-01T23:59:59\");";

        var result = service.ReplaceCountdownEnd(js, "2026-05-15T23:59:59");

        Assert.Contains("new Date('2026-05-15T23:59:59')", result);
        Assert.Equal(2, result.Split("2026-05-15T23:59:59").Length - 1);
    }
}
