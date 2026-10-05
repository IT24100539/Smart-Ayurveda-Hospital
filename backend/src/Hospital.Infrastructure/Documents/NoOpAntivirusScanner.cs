using Hospital.Application.Abstractions;

namespace Hospital.Infrastructure.Documents;

/// <summary>
/// Development stand-in for <see cref="IAntivirusScanner"/>.
/// It does not inspect bytes. Replace this registration in production with a scanner that
/// submits the upload to the hospital antivirus service. <see cref="MedicalDocumentStore"/>
/// calls the scanner after type and size checks and before it writes the file.
/// </summary>
public sealed class NoOpAntivirusScanner : IAntivirusScanner
{
    public Task ScanAsync(ReadOnlyMemory<byte> content, string contentType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
