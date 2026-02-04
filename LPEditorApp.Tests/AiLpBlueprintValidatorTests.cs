using System.Text.Json;
using System.Collections.Generic;
using LPEditorApp.Models.Ai;
using LPEditorApp.Services.Ai;
using Xunit;

namespace LPEditorApp.Tests;

public class AiLpBlueprintValidatorTests
{
    [Fact]
    public void Validate_ValidBlueprint_Passes()
    {
        var blueprint = BuildValidBlueprint();
        var validator = new AiLpBlueprintValidator();

        var result = validator.Validate(blueprint);

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        Assert.NotNull(result.NormalizedBlueprint);
    }

    [Fact]
    public void Validate_MissingRequiredSection_Fails()
    {
        var blueprint = BuildValidBlueprint();
        blueprint.Sections.RemoveAll(section => section.Type == "notes");
        var validator = new AiLpBlueprintValidator();

        var result = validator.Validate(blueprint);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("notes"));
    }

    [Fact]
    public void Validate_UnknownField_Fails()
    {
        var json = """
        {
          "meta": {
            "language": "ja",
            "title": "テスト",
            "tone": "casual",
            "goal": "acquisition",
            "industry": "小売",
            "brand": { "name": "テスト", "colorHint": null },
            "extra": "ng"
          },
          "sections": []
        }
        """;

        using var doc = JsonDocument.Parse(json);
        var blueprint = doc.RootElement.Deserialize<LpBlueprint>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var validator = new AiLpBlueprintValidator();

        var result = validator.Validate(blueprint, doc.RootElement);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("unknown meta field"));
    }

    [Fact]
    public void Validate_LengthViolation_WarnsAndNormalizes()
    {
        var blueprint = BuildValidBlueprint();
        blueprint.Sections[0].Props.Heading = new string('あ', 41);
        var validator = new AiLpBlueprintValidator();

        var result = validator.Validate(blueprint);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("heading"));
        Assert.NotNull(result.NormalizedBlueprint);
        Assert.True(result.NormalizedBlueprint!.Sections[0].Props.Heading!.Length <= 40);
    }

    [Fact]
    public void Validate_BulletsShort_Warns()
    {
        var blueprint = BuildValidBlueprint();
        blueprint.Sections[0].Props.Bullets = new List<string>();
        var validator = new AiLpBlueprintValidator();

        var result = validator.Validate(blueprint);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, warning => warning.Contains("bullets不足"));
    }

    private static LpBlueprint BuildValidBlueprint()
    {
        return new LpBlueprint
        {
            Meta = new LpBlueprintMeta
            {
                Language = "ja",
                Title = "テストLP",
                Tone = "casual",
                Goal = "acquisition",
                Industry = "小売",
                Brand = new LpBlueprintBrand { Name = "テストブランド", ColorHint = null }
            },
            Sections = new List<LpBlueprintSection>
            {
                BuildSection("hero"),
                BuildSection("offer"),
                BuildSection("howto"),
                BuildSection("notes"),
                BuildSection("footer")
            }
        };
    }

    private static LpBlueprintSection BuildSection(string type)
    {
        var bullets = new List<string> { "箇条書き1", "箇条書き2", "箇条書き3" };
        var items = new List<LpBlueprintItem>
        {
            new() { Title = "特典内容", Text = "内容", Badge = null }
        };
        if (type == "notes")
        {
            bullets = new List<string> { "注意1", "注意2", "注意3", "注意4", "注意5" };
        }
        if (type == "offer")
        {
            items = new List<LpBlueprintItem>
            {
                new() { Title = "特典内容", Text = "内容", Badge = null },
                new() { Title = "利用条件", Text = "条件", Badge = null },
                new() { Title = "期間", Text = "期間", Badge = null }
            };
        }

        return new LpBlueprintSection
        {
            Type = type,
            Id = $"{type}-1",
            Props = new LpBlueprintSectionProps
            {
                Heading = "見出し",
                Subheading = "サブ見出し",
                Body = "本文",
                Bullets = bullets,
                CtaText = "詳しく見る",
                Disclaimer = "注記",
                Items = items
            }
        };
    }
}
