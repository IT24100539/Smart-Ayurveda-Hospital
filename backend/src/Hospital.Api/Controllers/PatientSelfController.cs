using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Audit;
using Hospital.Application.Patients;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

/// <summary>
/// Read-only endpoints for the signed-in patient's own data.
/// All actions require the Patient role; identity is resolved server-side.
/// </summary>
[ApiController]
[Authorize(Roles = "Patient")]
[Route("api/patients/me")]
public sealed class PatientSelfController : ControllerBase
{
    private readonly IActorContext _actors;
    private readonly IPatientService _patients;
    private readonly IAppointmentService _appointments;
    private readonly IAuditLogService _audit;
    private readonly IClock _clock;

    public PatientSelfController(
        IActorContext actors,
        IPatientService patients,
        IAppointmentService appointments,
        IAuditLogService audit,
        IClock clock)
    {
        _actors = actors;
        _patients = patients;
        _appointments = appointments;
        _audit = audit;
        _clock = clock;
    }

    /// <summary>
    /// GET /api/patients/me/treatment-plans
    /// Returns the patient's appointments grouped as a lightweight treatment-plan list.
    /// Derived from real appointment records; no separate treatment-plan entity exists.
    /// </summary>
    [HttpGet("treatment-plans")]
    public async Task<ActionResult<IReadOnlyList<TreatmentPlanDto>>> TreatmentPlans(CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        // Fetch up to 200 appointments for this patient (covers all realistic plans)
        var paged = await _appointments.ListAsync(null, patient.Id, null, 1, 200, cancellationToken);
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        var plans = paged.Items
            .GroupBy(a => a.TreatmentId)
            .Select(g =>
            {
                var sessions = g.OrderBy(a => a.RequestedDate).ToList();
                var latest = sessions.MaxBy(a => a.RequestedDate);
                return new TreatmentPlanDto(
                    TreatmentId: g.Key,
                    TreatmentName: sessions[0].TreatmentName,
                    SessionCount: sessions.Count,
                    LastStatus: latest!.Status,
                    NextDate: sessions
                        .Where(a => a.RequestedDate >= today && (a.Status is
                            Hospital.Domain.Enums.AppointmentStatus.Pending or
                            Hospital.Domain.Enums.AppointmentStatus.Approved))
                        .Select(a => (DateOnly?)a.RequestedDate)
                        .FirstOrDefault(),
                    Sessions: sessions.Select(a => new TreatmentSessionDto(
                        a.Id,
                        a.RequestedDate,
                        a.RequestedTimeSlot,
                        a.Status)).ToList());
            })
            .ToList();

        await _audit.RecordAsync(AuditActions.View, AuditEntities.ClinicalRecord, patient.Id.ToString(), cancellationToken);
        return Ok(plans);
    }

    /// <summary>
    /// GET /api/patients/me/registration-summary
    /// Returns the patient's profile fields needed by the Flutter app's profile screen.
    /// </summary>
    [HttpGet("registration-summary")]
    public async Task<ActionResult<RegistrationSummaryDto>> RegistrationSummary(CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var dto = await _patients.GetByIdAsync(patient.Id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.Patient, patient.Id.ToString(), cancellationToken);

        return Ok(new RegistrationSummaryDto(
            Uhid: dto.Uhid,
            FullName: $"{dto.FirstName} {dto.LastName}",
            Phone: dto.Phone,
            Email: dto.Email,
            DateOfBirth: dto.DateOfBirth,
            Gender: dto.Gender,
            Prakriti: dto.Prakriti,
            Vikriti: dto.Vikriti,
            Allergies: dto.Allergies,
            BloodGroup: dto.BloodGroup));
    }
}

// ---------------------------------------------------------------------------
// Response DTOs (local to this controller — no domain coupling)
// ---------------------------------------------------------------------------

public sealed record TreatmentPlanDto(
    Guid TreatmentId,
    string TreatmentName,
    int SessionCount,
    Hospital.Domain.Enums.AppointmentStatus LastStatus,
    DateOnly? NextDate,
    IReadOnlyList<TreatmentSessionDto> Sessions);

public sealed record TreatmentSessionDto(
    Guid AppointmentId,
    DateOnly Date,
    string TimeSlot,
    Hospital.Domain.Enums.AppointmentStatus Status);

public sealed record RegistrationSummaryDto(
    string Uhid,
    string FullName,
    string Phone,
    string? Email,
    DateOnly DateOfBirth,
    Gender Gender,
    Hospital.Domain.Enums.DoshaType Prakriti,
    Hospital.Domain.Enums.DoshaType Vikriti,
    string? Allergies,
    string? BloodGroup);
