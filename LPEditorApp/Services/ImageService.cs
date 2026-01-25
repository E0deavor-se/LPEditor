using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using LPEditorApp.Utils;
using ILogger = LPEditorApp.Utils.ILogger;
using AppImageProcessingException = LPEditorApp.Utils.ImageProcessingException;

namespace LPEditorApp.Services;

public class ImageService
{
    private readonly ILogger _logger;

    public ImageService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ResizePngAsync(byte[] input, int maxWidth)
    {
        try
        {
            using var inputStream = new MemoryStream(input);
            using var image = await Image.LoadAsync(inputStream);

            if (image.Width > maxWidth)
            {
                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth, 0)
                };

                image.Mutate(x => x.Resize(resizeOptions));
            }

            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level6,
                FilterMethod = PngFilterMethod.Adaptive
            };

            using var ms = new MemoryStream();
            await image.SaveAsync(ms, encoder);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error($"PNG処理に失敗: {ex.Message}");
            throw new AppImageProcessingException("PNGのリサイズに失敗しました", ex);
        }
    }

    public ImageMeta GetImageInfo(byte[] input, string? path = null)
    {
        try
        {
            if (input is null || input.Length == 0)
            {
                _logger.Warn($"画像情報取得: 空データ path={path ?? "(null)"}");
                return new ImageMeta
                {
                    Width = 0,
                    Height = 0,
                    Bytes = input?.LongLength ?? 0,
                    Format = Path.GetExtension(path ?? string.Empty).TrimStart('.').ToUpperInvariant()
                };
            }

            if (LooksLikeDataUrl(input))
            {
                _logger.Warn($"画像情報取得: dataURL文字列の可能性 path={path ?? "(null)"} len={input.LongLength} head={ToHex(input, 16)}");
                return new ImageMeta
                {
                    Width = 0,
                    Height = 0,
                    Bytes = input.LongLength,
                    Format = "DATAURL"
                };
            }

            if (LooksLikeSvg(input))
            {
                _logger.Info($"画像情報取得: SVGを検出 path={path ?? "(null)"} len={input.LongLength}");
                return new ImageMeta
                {
                    Width = 0,
                    Height = 0,
                    Bytes = input.LongLength,
                    Format = "SVG"
                };
            }

            if (!HasKnownImageSignature(input))
            {
                _logger.Warn($"画像情報取得: 署名不一致 path={path ?? "(null)"} len={input.LongLength} head={ToHex(input, 16)}");
                return new ImageMeta
                {
                    Width = 0,
                    Height = 0,
                    Bytes = input.LongLength,
                    Format = Path.GetExtension(path ?? string.Empty).TrimStart('.').ToUpperInvariant()
                };
            }

            using var inputStream = new MemoryStream(input);
            var format = Image.DetectFormat(inputStream);
            inputStream.Position = 0;
            var imageInfo = Image.Identify(inputStream);
            if (imageInfo is null)
            {
                throw new InvalidOperationException("画像情報を取得できませんでした");
            }

            return new ImageMeta
            {
                Width = imageInfo.Width,
                Height = imageInfo.Height,
                Bytes = input.LongLength,
                Format = !string.IsNullOrWhiteSpace(format?.Name)
                    ? format.Name
                    : (Path.GetExtension(path ?? string.Empty).TrimStart('.').ToUpperInvariant())
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"画像情報取得に失敗: {ex.Message} path={path ?? "(null)"} len={input?.LongLength ?? 0} head={ToHex(input, 16)}");
            return new ImageMeta
            {
                Width = 0,
                Height = 0,
                Bytes = input?.LongLength ?? 0,
                Format = Path.GetExtension(path ?? string.Empty).TrimStart('.').ToUpperInvariant()
            };
        }
    }

    private static bool LooksLikeDataUrl(byte[] input)
    {
        if (input.Length < 5)
        {
            return false;
        }

        return input[0] == (byte)'d'
            && input[1] == (byte)'a'
            && input[2] == (byte)'t'
            && input[3] == (byte)'a'
            && input[4] == (byte)':';
    }

    private static bool LooksLikeSvg(byte[] input)
    {
        var sampleLength = Math.Min(input.Length, 256);
        var text = System.Text.Encoding.UTF8.GetString(input, 0, sampleLength);
        return text.Contains("<svg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasKnownImageSignature(byte[] input)
    {
        if (input.Length < 12)
        {
            return false;
        }

        // PNG
        if (input.Length >= 8
            && input[0] == 0x89 && input[1] == 0x50 && input[2] == 0x4E && input[3] == 0x47
            && input[4] == 0x0D && input[5] == 0x0A && input[6] == 0x1A && input[7] == 0x0A)
        {
            return true;
        }

        // JPEG
        if (input[0] == 0xFF && input[1] == 0xD8)
        {
            return true;
        }

        // GIF
        if (input[0] == 0x47 && input[1] == 0x49 && input[2] == 0x46)
        {
            return true;
        }

        // WEBP (RIFF....WEBP)
        if (input[0] == 0x52 && input[1] == 0x49 && input[2] == 0x46 && input[3] == 0x46
            && input[8] == 0x57 && input[9] == 0x45 && input[10] == 0x42 && input[11] == 0x50)
        {
            return true;
        }

        // BMP
        if (input[0] == 0x42 && input[1] == 0x4D)
        {
            return true;
        }

        return false;
    }

    private static string ToHex(byte[]? input, int maxBytes)
    {
        if (input is null || input.Length == 0)
        {
            return string.Empty;
        }

        var len = Math.Min(input.Length, maxBytes);
        return string.Join(" ", input.Take(len).Select(b => b.ToString("X2")));
    }
}

public class ImageMeta
{
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }
    public string Format { get; set; } = string.Empty;
}
