using Hospital.Application.Billing.Dtos;
using Hospital.Application.Common;
using Hospital.Domain.Entities;

namespace Hospital.Application.Billing;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> ListAsync(
        Guid? patientId,
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<InvoiceDto>> ListMineAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<InvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken);

    Task<InvoiceDto> UpdateDraftAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken);

    Task<InvoiceDto> IssueAsync(Guid id, CancellationToken cancellationToken);

    Task<InvoiceDto> CancelAsync(Guid id, CancellationToken cancellationToken);

    Task<InvoiceDto> RecordPaymentAsync(Guid id, RecordPaymentRequest request, CancellationToken cancellationToken);
}
