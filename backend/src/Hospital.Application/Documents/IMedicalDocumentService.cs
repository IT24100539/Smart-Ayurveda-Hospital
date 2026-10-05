using Hospital.Application.Common;
using Hospital.Application.Documents.Dtos;

namespace Hospital.Application.Documents;

public interface IMedicalDocumentService
{
    Task<PagedResult<MedicalDocumentDto>> ListForPatientAsync(
        Guid patientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<MedicalDocumentDto>> ListMineAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<MedicalDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<MedicalDocumentFile> OpenAsync(Guid id, CancellationToken cancellationToken);

    Task<MedicalDocumentDto> UploadAsync(
        UploadMedicalDocumentRequest request,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);
}
