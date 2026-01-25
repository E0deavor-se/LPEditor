namespace LPEditorApp.Utils;

public class AppErrorContext
{
    public bool HasError { get; set; }
    public string ErrorTitle { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public void SetError(string title, string message)
    {
        HasError = true;
        ErrorTitle = title;
        ErrorMessage = message;
    }

    public void Clear()
    {
        HasError = false;
        ErrorTitle = string.Empty;
        ErrorMessage = string.Empty;
    }
}

public class AppException : Exception
{
    public string UserMessage { get; }

    public AppException(string userMessage, string? technicalMessage = null, Exception? inner = null)
        : base(technicalMessage ?? userMessage, inner)
    {
        UserMessage = userMessage;
    }
}

public class TemplateException : AppException
{
    public TemplateException(string message, Exception? inner = null)
        : base($"テンプレート処理エラー: {message}", null, inner)
    {
    }
}

public class ZipExportException : AppException
{
    public ZipExportException(string message, Exception? inner = null)
        : base($"ZIP出力エラー: {message}", null, inner)
    {
    }
}

public class ImageProcessingException : AppException
{
    public ImageProcessingException(string message, Exception? inner = null)
        : base($"画像処理エラー: {message}", null, inner)
    {
    }
}
