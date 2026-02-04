using System.Text.Json.Serialization;

namespace LPEditorApp.Models.Ai;

public class LpReferenceStyleSpec
{
    [JsonPropertyName("styleTokens")]
    public LpStyleTokens StyleTokens { get; set; } = new();

    [JsonPropertyName("layoutRecipe")]
    public LpLayoutRecipe LayoutRecipe { get; set; } = new();

    [JsonPropertyName("decorSpec")]
    public LpDecorSpec DecorSpec { get; set; } = new();
}

public class LpStyleTokens
{
    [JsonPropertyName("colors")]
    public LpColorTokens Colors { get; set; } = new();

    [JsonPropertyName("typography")]
    public LpTypographyTokens Typography { get; set; } = new();

    [JsonPropertyName("spacing")]
    public LpSpacingTokens Spacing { get; set; } = new();

    [JsonPropertyName("radius")]
    public LpRadiusTokens Radius { get; set; } = new();

    [JsonPropertyName("shadow")]
    public LpShadowTokens Shadow { get; set; } = new();
}

public class LpColorTokens
{
    [JsonPropertyName("primary")]
    public string Primary { get; set; } = "#1E3A8A";

    [JsonPropertyName("accent")]
    public string Accent { get; set; } = "#F59E0B";

    [JsonPropertyName("bg")]
    public string Bg { get; set; } = "#F8FAFC";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "#0F172A";

    [JsonPropertyName("muted")]
    public string Muted { get; set; } = "#64748B";

    [JsonPropertyName("border")]
    public string Border { get; set; } = "#E2E8F0";
}

public class LpTypographyTokens
{
    [JsonPropertyName("h1")]
    public int H1 { get; set; } = 32;

    [JsonPropertyName("h2")]
    public int H2 { get; set; } = 24;

    [JsonPropertyName("body")]
    public int Body { get; set; } = 16;

    [JsonPropertyName("small")]
    public int Small { get; set; } = 13;

    [JsonPropertyName("weightScale")]
    public string WeightScale { get; set; } = "medium";
}

public class LpSpacingTokens
{
    [JsonPropertyName("sectionY")]
    public int SectionY { get; set; } = 32;

    [JsonPropertyName("cardPadding")]
    public int CardPadding { get; set; } = 24;

    [JsonPropertyName("gridGap")]
    public int GridGap { get; set; } = 16;
}

public class LpRadiusTokens
{
    [JsonPropertyName("card")]
    public int Card { get; set; } = 16;

    [JsonPropertyName("button")]
    public int Button { get; set; } = 999;

    [JsonPropertyName("badge")]
    public int Badge { get; set; } = 999;
}

public class LpShadowTokens
{
    [JsonPropertyName("card")]
    public string Card { get; set; } = "soft";

    [JsonPropertyName("sticky")]
    public string Sticky { get; set; } = "soft";
}

public class LpLayoutRecipe
{
    [JsonPropertyName("hero")]
    public string Hero { get; set; } = "kv-image-top";

    [JsonPropertyName("section")]
    public string Section { get; set; } = "card";

    [JsonPropertyName("heading")]
    public string Heading { get; set; } = "band";

    [JsonPropertyName("ranking")]
    public string Ranking { get; set; } = "table";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "accordion";
}

public class LpDecorSpec
{
    [JsonPropertyName("background")]
    public string Background { get; set; } = "solid";

    [JsonPropertyName("divider")]
    public string Divider { get; set; } = "none";

    [JsonPropertyName("badge")]
    public string Badge { get; set; } = "none";
}
