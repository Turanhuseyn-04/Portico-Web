namespace Portico.Web.Services;

public enum MediaKind
{
    SlideImage,
    Video
}

public sealed record MediaSaveResult(string? Path, string? Error);

// Saves files uploaded in the admin under wwwroot/uploads, where the site serves them, and removes them again.
// A file is accepted only when its first bytes show it really is an allowed image or video, whatever its name says,
// and it gets a new random name, so an upload can never overwrite another file or land outside the folder.
public interface IMediaStore
{
    Task<MediaSaveResult> SaveAsync(IFormFile? file, MediaKind kind, CancellationToken cancellationToken);
    void DeleteIfUploaded(string? path);
}

public class MediaStore : IMediaStore
{
    public const long MaxImageBytes = 10L * 1024 * 1024;
    public const long MaxVideoBytes = 200L * 1024 * 1024;

    public const string ImageAccept = ".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp";
    public const string VideoAccept = ".mp4,.webm,video/mp4,video/webm";

    private const string UrlRoot = "/uploads/";

    private static readonly HashSet<string> Mp4Brands = new(StringComparer.Ordinal)
    {
        "isom", "iso2", "iso4", "iso5", "iso6", "mp41", "mp42", "avc1", "M4V ", "dash", "mmp4", "MSNV"
    };

    private readonly string _root;
    private readonly ILogger<MediaStore> _logger;

    public MediaStore(IWebHostEnvironment environment, ILogger<MediaStore> logger)
    {
        _root = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
        _logger = logger;
    }

    public async Task<MediaSaveResult> SaveAsync(IFormFile? file, MediaKind kind, CancellationToken cancellationToken)
    {
        var what = kind == MediaKind.Video ? "video" : "image";
        if (file is null || file.Length == 0) return new(null, kind == MediaKind.Video ? "Choose a video file." : "Choose an image file.");

        var max = kind == MediaKind.Video ? MaxVideoBytes : MaxImageBytes;
        if (file.Length > max) return new(null, $"The {what} must be {max / (1024 * 1024)} MB or smaller.");

        var header = new byte[16];
        int read;
        await using (var stream = file.OpenReadStream())
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

        var extension = Detect(header.AsSpan(0, read), kind);
        if (extension is null)
            return new(null, kind == MediaKind.Video
                ? "The video must be an MP4 or WebM file."
                : "The image must be a JPEG, PNG, or WebP file.");

        var folder = kind == MediaKind.Video ? "videos" : "slides";
        var name = Guid.NewGuid().ToString("N") + extension;
        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        var target = Path.Combine(directory, name);
        await using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await file.CopyToAsync(output, cancellationToken);

        _logger.LogInformation("Saved an uploaded {Kind} as {Path} ({Bytes} bytes).", kind, UrlRoot + folder + "/" + name, file.Length);
        return new($"{UrlRoot}{folder}/{name}", null);
    }

    // Only files this store saved are removed; the site's own /assets are never touched.
    public void DeleteIfUploaded(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(UrlRoot, StringComparison.Ordinal)) return;

        var full = Path.GetFullPath(Path.Combine(_root, path[UrlRoot.Length..].Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (File.Exists(full)) File.Delete(full);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete the uploaded file {Path}.", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Could not delete the uploaded file {Path}.", path);
        }
    }

    // The extension to save under, decided by the file's signature.
    private static string? Detect(ReadOnlySpan<byte> head, MediaKind kind)
    {
        if (kind == MediaKind.SlideImage)
        {
            if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return ".jpg";
            if (head.Length >= 8 && head[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
            if (head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8)) return ".webp";
            return null;
        }

        // "ftyp" also starts HEIC/AVIF images and QuickTime files, so the brand after it must be an MP4 one.
        if (head.Length >= 12 && head[4..8].SequenceEqual("ftyp"u8) && Mp4Brands.Contains(System.Text.Encoding.ASCII.GetString(head[8..12]))) return ".mp4";
        if (head.Length >= 4 && head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3) return ".webm";
        return null;
    }
}
