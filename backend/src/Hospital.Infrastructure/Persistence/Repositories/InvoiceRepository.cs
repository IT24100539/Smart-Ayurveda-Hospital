using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly HospitalDbContext _db;

    public InvoiceRepository(HospitalDbContext db) => _db = db;

    public Task<Invoice?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = _db.Invoices
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Payments)
            .AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(
        Guid? patientId,
        InvoiceStatus? status,
        bool patientVisibleOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Invoices.AsNoTracking()
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Payments)
            .AsQueryable();

        if (patientId is Guid ownerId)
        {
            query = query.Where(invoice => invoice.PatientId == ownerId);
        }

        if (patientVisibleOnly)
        {
            query = query.Where(invoice =>
                invoice.Status == InvoiceStatus.Issued || invoice.Status == InvoiceStatus.Paid);
        }
        else if (status is InvoiceStatus filtered)
        {
            query = query.Where(invoice => invoice.Status == filtered);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ThenByDescending(invoice => invoice.InvoiceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> IsSourceOpenAsync(string openSourceKey, Guid? exceptInvoiceId, CancellationToken cancellationToken) =>
        _db.InvoiceLines.AnyAsync(
            line => line.OpenSourceKey == openSourceKey
                && (exceptInvoiceId == null || line.InvoiceId != exceptInvoiceId),
            cancellationToken);

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken) =>
        await _db.Invoices.AddAsync(invoice, cancellationToken);

    public async Task AddPaymentAsync(InvoicePayment payment, CancellationToken cancellationToken) =>
        await _db.InvoicePayments.AddAsync(payment, cancellationToken);

    public void ReplaceLines(Invoice invoice, IReadOnlyList<InvoiceLine> lines)
    {
        if (invoice.Lines.Count > 0)
        {
            _db.InvoiceLines.RemoveRange(invoice.Lines);
            invoice.Lines.Clear();
        }

        foreach (var line in lines)
        {
            line.InvoiceId = invoice.Id;
            _db.InvoiceLines.Add(line);
        }

        _db.Entry(invoice).Property(x => x.UpdatedAt).IsModified = true;
    }
}
