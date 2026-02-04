using System.Text.Json;
using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using Xunit;

namespace LPEditorApp.Tests;

public class AiDesignValidatorTests
{
    [Fact]
    public void Validate_InvalidEnum_IsHardError()
    {
        var spec = BuildValid();
        spec.DesignType = "invalid";
        var validator = new AiDesignValidator();

        var result = validator.Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("designType"));
    }

    [Fact]
    public void Validate_InvalidColor_IsHardError()
    {
        var spec = BuildValid();
        spec.Theme.Primary = "red";
        var validator = new AiDesignValidator();

        var result = validator.Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("theme.primary"));
    }

    [Fact]
    public void Validate_RadiusOutOfRange_IsHardError()
    {
        var spec = BuildValid();
        spec.Theme.Radius = 80;
        var validator = new AiDesignValidator();

        var result = validator.Validate(spec);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("radius"));
    }

    [Fact]
    public void Validate_Defaults_AreAppliedAsWarnings()
    {
        var json = """
        {
          "version": 1,
          "designType": "coupon",
          "theme": {
            "primary": "#112233",
            "secondary": "#223344",
            "accent": "#334455",
            "bg": "#445566",
            "text": "#556677",
            "radius": 16,
            "shadow": "unknown",
            "font": "unknown",
            "ctaStyle": "unknown"
          },
          "layout": {
            "container": "centered",
            "hero": "split",
            "sectionStyle": "card",
            "headingStyle": "bold",
            "offerStyle": "singleCard",
            "howtoStyle": "steps",
            "notesStyle": "boxed",
            "rankingStyle": "table"
          }
        }
        """;

        var validator = new AiDesignValidator();
        using var doc = JsonDocument.Parse(json);
        var spec = doc.RootElement.Deserialize<LpDesignSpec>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var result = validator.Validate(spec, doc.RootElement);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("shadow"));
        Assert.Contains(result.Warnings, warning => warning.Contains("font"));
        Assert.Contains(result.Warnings, warning => warning.Contains("ctaStyle"));
        Assert.NotNull(result.NormalizedSpec);
    }

    private static LpDesignSpec BuildValid()
    {
        return new LpDesignSpec
        {
            Version = 1,
            DesignType = "coupon",
            Theme = new LpDesignTheme
            {
                Primary = "#112233",
                Secondary = "#223344",
                Accent = "#334455",
                Bg = "#f8fafc",
                Text = "#0f172a",
                Radius = 16,
                Shadow = "soft",
                Font = "system",
                CtaStyle = "solid"
            },
            Layout = new LpDesignLayout
            {
                Container = "centered",
                Hero = "split",
                SectionStyle = "card",
                HeadingStyle = "bold",
                OfferStyle = "singleCard",
                HowtoStyle = "steps",
                NotesStyle = "boxed",
                RankingStyle = "table"
            }
        };
    }
}
