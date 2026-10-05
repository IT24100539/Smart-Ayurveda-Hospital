using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Hospital.Infrastructure.Persistence;

/// <summary>
/// Adds one audit row in the same SaveChanges as a patient, appointment, or clinical write.
/// View traffic is recorded separately and is not one row per list item.
/// </summary>
public sealed class ClinicalAuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _user;
    private readonly IClientAddress _client;
    private bool _writing;

    public ClinicalAuditInterceptor(ICurrentUser user, IClientAddress client)
    {
        _user = user;
        _client = client;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Append(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Append(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Append(DbContext? context)
    {
        if (context is null || _writing)
        {
            return;
        }

        _writing = true;
        try
        {
            foreach (var audit in context.ChangeTracker.Entries<AuditLog>().ToList())
            {
                if (audit.State is EntityState.Added or EntityState.Modified)
                {
                    audit.Entity.Details = AuditDetailsSanitizer.Sanitize(audit.Entity.Details);
                }
            }

            if (!_user.IsAuthenticated)
            {
                return;
            }

            var changes = context.ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(entry => (Entry: entry, EntityName: Classify(entry.Metadata.ClrType)))
                .Where(change => change.EntityName is not null)
                .ToList();

            if (changes.Count == 0)
            {
                return;
            }

            var actorUserId = _user.UserId;
            var actorEmail = _user.Email;
            var actorRole = _user.Role.ToString();
            var ipAddress = _client.IpAddress;

            foreach (var (entry, entityName) in changes)
            {
                context.Add(new AuditLog
                {
                    ActorUserId = actorUserId,
                    ActorEmail = actorEmail,
                    ActorRole = actorRole,
                    Action = entry.State switch
                    {
                        EntityState.Added => AuditActions.Create,
                        EntityState.Deleted => AuditActions.Delete,
                        _ => AuditActions.Update
                    },
                    EntityName = entityName!,
                    EntityId = entry.Property("Id").CurrentValue?.ToString() ?? string.Empty,
                    TargetEmail = string.Empty,
                    Details = string.Empty,
                    IpAddress = ipAddress
                });
            }
        }
        finally
        {
            _writing = false;
        }
    }

    private static string? Classify(Type type)
    {
        if (type == typeof(Patient))
        {
            return AuditEntities.Patient;
        }

        if (type == typeof(Appointment))
        {
            return AuditEntities.Appointment;
        }

        if (type == typeof(Prescription) || type == typeof(PrescriptionRevision) || type == typeof(MedicalDocument))
        {
            return AuditEntities.ClinicalRecord;
        }

        if (type == typeof(Invoice))
        {
            return AuditEntities.Invoice;
        }

        return null;
    }
}
