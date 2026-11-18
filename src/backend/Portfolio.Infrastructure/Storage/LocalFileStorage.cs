using Microsoft.Extensions.Options;
using Portfolio.Application.Admin.Content;

namespace Portfolio.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Absolute folder where uploaded files are written.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Public URL prefix the API serves the folder under.</summary>
    public string RequestPath { get; set; } = "/uploads";

    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>
/// Stores uploads on the local disk. The file type is decided by its signature (magic bytes),
/// never by the client's file name or Content-Type, and files get random names.
/// </summary>
internal sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private const int HeaderLength = 12;

    private static readonly (string Extension, string ContentType, Func<byte[], bool> Matches)[] Signatures =
    [
        (".png", "image/png", h => h is [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..]),
        (".jpg", "image/jpeg", h => h is [0xFF, 0xD8, 0xFF, ..]),
        (".gif", "image/gif", h => h is [0x47, 0x49, 0x46, 0x38, ..]),
        (".webp", "image/webp", h => h is [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..]),
        (".pdf", "application/pdf", h => h is [0x25, 0x50, 0x44, 0x46, 0x2D, ..]),
    ];

    public async Task<StoredFile?> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var header = new byte[HeaderLength];
        var headerLength = await content.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false, cancellationToken);
        if (headerLength < 4)
        {
            return null;
        }

        var signature = Signatures.FirstOrDefault(s => s.Matches(header));
        if (signature.Extension is null)
        {
            return null;
        }

        Directory.CreateDirectory(settings.RootPath);
        var fileName = $"{Guid.NewGuid():N}{signature.Extension}";
        var path = Path.Combine(settings.RootPath, fileName);

        long size = headerLength;
        try
        {
            await using (var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await target.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);

                var buffer = new byte[81920];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    size += read;
                    if (size > settings.MaxBytes)
                    {
                        throw new FileTooLargeException();
                    }

                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }
        }
        catch
        {
            File.Delete(path);
            throw;
        }

        return new StoredFile($"{settings.RequestPath.TrimEnd('/')}/{fileName}", signature.ContentType, size);
    }
}

public sealed class FileTooLargeException() : Exception("The uploaded file exceeds the maximum allowed size.");
