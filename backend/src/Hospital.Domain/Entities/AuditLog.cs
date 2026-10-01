using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid TargetUserId { get; set; }
    public string TargetEmail { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
