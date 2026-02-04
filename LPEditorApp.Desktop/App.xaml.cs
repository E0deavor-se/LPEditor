using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using LPEditorApp.Services;
using LPEditorApp.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LPEditorApp.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public IServiceProvider Services { get; }

	public static IServiceProvider? ServicesProvider { get; private set; }

	public App()
	{
		RegisterGlobalExceptionHandlers();
		EnsureWebView2Runtime();

		var services = new ServiceCollection();
		services.AddWpfBlazorWebView();
#if DEBUG
		services.AddBlazorWebViewDeveloperTools();
#endif

		services.AddLogging(builder => builder.AddDebug());
		services.AddSingleton<IWebHostEnvironment>(_ =>
		{
			var baseDir = AppDomain.CurrentDomain.BaseDirectory;
			var webRoot = Path.Combine(baseDir, "wwwroot");
			return new DesktopWebHostEnvironment(baseDir, webRoot);
		});

		services.AddScoped<LPEditorApp.Utils.ILogger, ConsoleLogger>();
		services.AddScoped<TemplateService>();
		services.AddScoped<PreviewService>();
		services.AddScoped<JsReplacementService>();
		services.AddScoped<ImageService>();
		services.AddScoped<ContentPersistService>();
		services.AddScoped<ZipExportService>();
		services.AddScoped<EditorState>();
		services.AddScoped<BackgroundPresetService>();
		services.AddScoped<IHighlightService, HighlightService>();
		services.AddScoped<FramePresetService>();
		services.AddScoped<AnimationPresetService>();
		services.AddScoped<LpImportService>();

		Services = services.BuildServiceProvider();
		ServicesProvider = Services;
	}

	private void RegisterGlobalExceptionHandlers()
	{
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			if (args.ExceptionObject is Exception ex)
			{
				LogFatal(ex);
			}
		};

		DispatcherUnhandledException += (_, args) =>
		{
			var ex = Unwrap(args.Exception);
			LogFatal(ex);
			MessageBox.Show($"{ex.GetType().Name}: {ex.Message}", "致命的なエラー", MessageBoxButton.OK, MessageBoxImage.Error);
			args.Handled = true;
		};
	}

	private static void EnsureWebView2Runtime()
	{
		try
		{
			_ = CoreWebView2Environment.GetAvailableBrowserVersionString();
		}
		catch (Exception ex)
		{
			LogFatal(ex);
			MessageBox.Show(
				"WebView2 ランタイムが見つかりません。\n" +
				"Microsoft Edge WebView2 Runtime をインストールしてください。",
				"起動できません",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
			Current?.Shutdown();
		}
	}

	private static Exception Unwrap(Exception ex)
	{
		return ex is TargetInvocationException tie && tie.InnerException is not null
			? tie.InnerException
			: ex.GetBaseException();
	}

	private static void LogFatal(Exception ex)
	{
		try
		{
			var localDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"LPEditorApp",
				"logs");
			Directory.CreateDirectory(localDir);
			var localPath = Path.Combine(localDir, "desktop-fatal.log");
			File.AppendAllText(localPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n");

			var baseDir = AppDomain.CurrentDomain.BaseDirectory;
			if (!string.IsNullOrWhiteSpace(baseDir))
			{
				var basePath = Path.Combine(baseDir, "desktop-fatal.log");
				File.AppendAllText(basePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n");
			}
		}
		catch
		{
			Debug.WriteLine(ex);
		}
	}
}

