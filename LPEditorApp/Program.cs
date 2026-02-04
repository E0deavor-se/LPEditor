using LPEditorApp.Components;
using LPEditorApp.Services;
using LPEditorApp.Services.Ai;
using LPEditorApp.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddControllers();

builder.Services.AddScoped<LPEditorApp.Utils.ILogger, ConsoleLogger>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<PreviewService>();
builder.Services.AddScoped<NativePreviewService>();
builder.Services.AddScoped<JsReplacementService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<ContentPersistService>();
builder.Services.AddScoped<ZipExportService>();
builder.Services.AddScoped<EditorState>();
builder.Services.AddScoped<BackgroundPresetService>();
builder.Services.AddScoped<IHighlightService, HighlightService>();
builder.Services.AddScoped<FramePresetService>();
builder.Services.AddScoped<AnimationPresetService>();
builder.Services.AddScoped<LpImportService>();
builder.Services.AddScoped<AiLpBlueprintValidator>();
builder.Services.AddScoped<AiLpBlueprintMapper>();
builder.Services.AddScoped<AiDesignValidator>();
builder.Services.AddScoped<AiDesignMapper>();
builder.Services.AddScoped<AiDecorationValidator>();
builder.Services.AddScoped<AiDecorationMapper>();
builder.Services.AddScoped<AiReferenceDesignValidator>();
builder.Services.AddScoped<AiReferenceStyleMapper>();
builder.Services.AddScoped<AiGenerateLpService>();
builder.Services.AddScoped<AiGenerateDesignService>();
builder.Services.AddScoped<AiGenerateDecorationService>();
builder.Services.AddScoped<AiGenerateReferenceDesignService>();
builder.Services.AddScoped<AiGenerateReferenceZipService>();
builder.Services.AddScoped<AiGenerateZipService>();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient<IAiChatClient, OpenAiChatClient>(client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("Ai:BaseUrl") ?? "https://api.openai.com/v1";
    var timeoutSeconds = builder.Configuration.GetValue<int?>("Ai:TimeoutSeconds") ?? 45;
    client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 120));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
