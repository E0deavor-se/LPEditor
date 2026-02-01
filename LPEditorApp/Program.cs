using LPEditorApp.Components;
using LPEditorApp.Services;
using LPEditorApp.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<LPEditorApp.Utils.ILogger, ConsoleLogger>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<PreviewService>();
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
