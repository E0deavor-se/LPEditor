using System.Text.Json.Serialization;

namespace LPEditorApp.Models.Ai;

public class LpDecorationSpec
{
    [JsonPropertyName("background")]
    public LpDecorationBackground Background { get; set; } = new();

    [JsonPropertyName("sectionFrame")]
    public LpDecorationSectionFrame SectionFrame { get; set; } = new();

    [JsonPropertyName("headingDecoration")]
    public LpDecorationHeading HeadingDecoration { get; set; } = new();

    [JsonPropertyName("ctaEmphasis")]
    public LpDecorationCta CtaEmphasis { get; set; } = new();

    [JsonPropertyName("sectionDivider")]
    public LpDecorationDivider SectionDivider { get; set; } = new();
}

public class LpDecorationBackground
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "solid";

    [JsonPropertyName("colors")]
    public List<string> Colors { get; set; } = new() { "#F8FAFC", "#FFFFFF" };

    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "none";

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.12;
}

public class LpDecorationSectionFrame
{
    [JsonPropertyName("style")]
    public string Style { get; set; } = "card";

    [JsonPropertyName("radius")]
    public int Radius { get; set; } = 16;

    [JsonPropertyName("shadow")]
    public string Shadow { get; set; } = "soft";

    [JsonPropertyName("border")]
    public string Border { get; set; } = "light";
}

public class LpDecorationHeading
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "accent-line";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#0E0D6A";

    [JsonPropertyName("thickness")]
    public int Thickness { get; set; } = 3;
}

public class LpDecorationCta
{
    [JsonPropertyName("style")]
    public string Style { get; set; } = "badge";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#F59E0B";
}

public class LpDecorationDivider
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "none";

    [JsonPropertyName("height")]
    public int Height { get; set; } = 0;

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#E2E8F0";
}
