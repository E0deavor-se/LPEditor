using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LPEditorApp.Desktop;

public sealed class DesktopWebHostEnvironment : IWebHostEnvironment
{
    public DesktopWebHostEnvironment(string contentRootPath, string webRootPath)
    {
        ContentRootPath = contentRootPath;
        WebRootPath = webRootPath;
        ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
    }

    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "LPEditorApp.Desktop";
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
}
