using LPEditorApp.Services.Ai;
using Xunit;

namespace LPEditorApp.Tests;

public class AiGenerateZipValidationTests
{
    [Fact]
    public void ValidateHtmlCss_ExternalUrl_IsRejected()
    {
        var html = "<link rel=\"stylesheet\" href=\"https://example.com/a.css\" />";
        var css = "body { color: #000; }";

        var errors = AiGenerateZipService.ValidateHtmlCss(html, css);

        Assert.Contains(errors, error => error.Contains("external url"));
    }

    [Fact]
    public void ValidateHtmlCss_ScriptTag_IsRejected()
    {
        var html = "<script>alert('x')</script>";
        var css = "body { color: #000; }";

        var errors = AiGenerateZipService.ValidateHtmlCss(html, css);

        Assert.Contains(errors, error => error.Contains("script"));
    }
}
