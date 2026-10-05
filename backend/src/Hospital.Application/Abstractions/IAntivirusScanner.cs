using Hospital.Domain.Exceptions;

namespace Hospital.Application.Abstractions;

/// <summary>
/// Malware scan hook for a medical document upload.
/// </summary>
/// <remarks>
/// <para>
/// The document store calls <see cref="ScanAsync"/> after the upload has passed the file-type and
/// size checks and before any byte is written to disk. That call is the only scan hook.
/// </para>
/// <para>
/// Development registers <c>NoOpAntivirusScanner</c>, which accepts every file and does not inspect it.
/// Production replaces that registration with an implementation that submits the bytes to the hospital
/// antivirus service. Throw <see cref="DomainException"/> when the file is infected or the scan cannot
/// complete. The store does not write the file when this method throws.
/// </para>
/// </remarks>
public interface IAntivirusScanner
{
    /// <summary>
    /// Scan one accepted upload before it is stored.
    /// </summary>
    Task ScanAsync(ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken);
}
