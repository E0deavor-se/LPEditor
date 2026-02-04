namespace LPEditorApp.Services.Ai;

public class AiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4.1-mini";
    public string ModelBlueprint { get; set; } = "gpt-4.1-mini";
    public string ModelDesignSpec { get; set; } = "gpt-4.1-mini";
    public string ModelDecorationSpec { get; set; } = "gpt-4.1-mini";
    public string ModelReferenceSpec { get; set; } = "gpt-4.1-mini";
    public string ModelReferenceZip { get; set; } = "gpt-4.1-mini";
    public string ModelExperimentalZip { get; set; } = "gpt-4.1";
    public int MaxRetries { get; set; } = 2;
    public int MaxRetryCount { get; set; } = 2;
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxInputLength { get; set; } = 2000;
    public bool EnableDryRun { get; set; }
    public int MaxAiResponseChars { get; set; } = 20000;
    public bool StrictJsonOnly { get; set; } = true;
}
