using System.Text.RegularExpressions;
using Hospital.Application.Abstractions;
using Hospital.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Hospital.Infrastructure.Documents;

public sealed partial class MedicalDocumentStore : IMedicalDocumentStore
{
    private readonly MedicalDocumentOptions _options;
    private readonly IAntivirusScanner _scanner;
    private readonly string _root;

    public MedicalDocumentStore(IOptions<MedicalDocumentOptions> options, IAntivirusScanner scanner)
    {
        _options = options.Value;
        _scanner = scanner;
        if (string.IsNullOrWhiteSpace(_options.StoragePath) || !Path.IsPathRooted(_options.StoragePath))
        {
            throw new InvalidOperationException("Medical document storage is not configured.");
        }

        _root = Path.GetFullPath(_options.StoragePath);
    }

    public async Task<StoredMedicalDocument> SaveAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadLimitedAsync(content, cancellationToken);
        var sniffed = Sniff(bytes)
            ?? throw new DomainException("Document must be a PDF, JPEG, PNG, WebP, or GIF.");
        var declared = NormalizeContentType(contentType);
        if (declared != sniffed)
        {
            throw new DomainException("Document contents do not match the declared file type.");
        }

        // Antivirus hook: scan accepted bytes before they touch disk.
        // NoOpAntivirusScanner is the development stand-in. A production IAntivirusScanner
        // replaces it and throws when the file is infected or the scan cannot complete.
        await _scanner.ScanAsync(bytes, sniffed, cancellationToken);

        var extension = sniffed switch
        {
            "application/pdf" => ".pdf",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        Directory.CreateDirectory(_root);
        var destination = SafePath(storageKey);
        var temporary = destination + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellationToken);
            File.Move(temporary, destination, overwrite: true);
        }
        catch
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }

            throw;
        }

        return new StoredMedicalDocument(storageKey, sniffed, bytes.Length);
    }

    public Stream? OpenRead(string storageKey)
    {
        if (!TrySafePath(storageKey, out var full) || !File.Exists(full))
        {
            return null;
        }

        return new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
    }

    public Task DeleteAsync(string? storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(storageKey) || !TrySafePath(storageKey, out var full))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }

    private async Task<byte[]> ReadLimitedAsync(Stream content, CancellationToken cancellationToken)
    {
        var max = _options.MaxBytes;
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        long total = 0;
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            total += read;
            if (total > max)
            {
                throw new DomainException($"Document must be a PDF, JPEG, PNG, WebP, or GIF no larger than {LimitLabel(max)}.");
            }

            buffer.Write(chunk, 0, read);
        }

        if (total == 0)
        {
            throw new DomainException("Document file is empty.");
        }

        return buffer.ToArray();
    }

    private bool TrySafePath(string storageKey, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey != Path.GetFileName(storageKey)
            || !StorageKeyPattern().IsMatch(storageKey))
        {
            return false;
        }

        var full = Path.GetFullPath(Path.Combine(_root, storageKey));
        var prefix = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return false;
        }

        fullPath = full;
        return true;
    }

    private string SafePath(string storageKey)
    {
        if (!TrySafePath(storageKey, out var full))
        {
            throw new InvalidOperationException("Medical document storage key is invalid.");
        }

        return full;
    }

    private static string LimitLabel(int maxBytes) =>
        maxBytes % (1024 * 1024) == 0
            ? $"{maxBytes / (1024 * 1024)} MB"
            : $"{Math.Max(1, maxBytes / 1024)} KB";

    private static string? Sniff(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 5
            && bytes[0] == (byte)'%'
            && bytes[1] == (byte)'P'
            && bytes[2] == (byte)'D'
            && bytes[3] == (byte)'F'
            && bytes[4] == (byte)'-')
        {
            return "application/pdf";
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 12
            && bytes[0] == (byte)'R'
            && bytes[1] == (byte)'I'
            && bytes[2] == (byte)'F'
            && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W'
            && bytes[9] == (byte)'E'
            && bytes[10] == (byte)'B'
            && bytes[11] == (byte)'P')
        {
            return "image/webp";
        }

        if (bytes.Length >= 6
            && bytes[0] == (byte)'G'
            && bytes[1] == (byte)'I'
            && bytes[2] == (byte)'F'
            && bytes[3] == (byte)'8'
            && (bytes[4] == (byte)'7' || bytes[4] == (byte)'9')
            && bytes[5] == (byte)'a')
        {
            return "image/gif";
        }

        return null;
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var media = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return media is "image/jpg" or "image/pjpeg" ? "image/jpeg" : media;
    }

    [GeneratedRegex("^[a-f0-9]{32}\\.(pdf|jpg|png|webp|gif)$", RegexOptions.CultureInvariant)]
    private static partial Regex StorageKeyPattern();
}
