using System.Text.Json.Serialization;

namespace LPEditorApp.Models.Ai;

public class LpDesignSpec
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("designType")]
    public string DesignType { get; set; } = string.Empty;

    [JsonPropertyName("theme")]
    public LpDesignTheme Theme { get; set; } = new();

    [JsonPropertyName("layout")]
    public LpDesignLayout Layout { get; set; } = new();
}

public class LpDesignTheme
{
    [JsonPropertyName("primary")]
    public string Primary { get; set; } = "#0e0d6a";

    [JsonPropertyName("secondary")]
    public string Secondary { get; set; } = "#1e293b";

    [JsonPropertyName("accent")]
    public string Accent { get; set; } = "#f59e0b";

    [JsonPropertyName("bg")]
    public string Bg { get; set; } = "#f8fafc";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "#0f172a";

    [JsonPropertyName("radius")]
    public int Radius { get; set; } = 16;

    [JsonPropertyName("shadow")]
    public string Shadow { get; set; } = "soft";

    [JsonPropertyName("font")]
    public string Font { get; set; } = "system";

    [JsonPropertyName("ctaStyle")]
    public string CtaStyle { get; set; } = "solid";
}

public class LpDesignLayout
{
    [JsonPropertyName("container")]
    public string Container { get; set; } = "centered";

    [JsonPropertyName("hero")]
    public string Hero { get; set; } = "split";

    [JsonPropertyName("sectionStyle")]
    public string SectionStyle { get; set; } = "card";

    [JsonPropertyName("headingStyle")]
    public string HeadingStyle { get; set; } = "bold";

    [JsonPropertyName("offerStyle")]
    public string OfferStyle { get; set; } = "singleCard";

    [JsonPropertyName("howtoStyle")]
    public string HowtoStyle { get; set; } = "steps";

    [JsonPropertyName("notesStyle")]
    public string NotesStyle { get; set; } = "boxed";

    [JsonPropertyName("rankingStyle")]
    public string RankingStyle { get; set; } = "table";
}
