using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Hospital.Api.Authentication;
using Hospital.Application.Appointments;
using Hospital.Application.Wards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Policy = InternalServiceAuthenticationHandler.PolicyName)]
public sealed class InternalSchedulingController(IWardService wards, TreatmentAvailabilityService treatments) : ControllerBase
{
    [HttpGet("api/internal/wards/{id:guid}/availability")]
    public async Task<ActionResult> WardAvailability(Guid id, CancellationToken cancellationToken)
    {
        var ward = await wards.GetOccupancyAsync(id, forPatient: true, cancellationToken);
        if (ward is null) return NotFound();
        return Ok(new { wardId = ward.Id, wardName = ward.Name, totalCapacity = ward.TotalCapacity,
            occupiedCapacity = ward.OccupiedBeds, freeCapacity = Math.Max(0, ward.TotalCapacity - ward.OccupiedBeds) });
    }

    [HttpPost("api/internal/admissions")]
    public async Task<ActionResult> RequestAdmission(InternalAdmissionRequest request, CancellationToken cancellationToken)
    {
        var patientId = request.PatientId ?? request.SnakePatientId;
        var wardId = request.WardId ?? request.SnakeWardId;
        var date = request.PreferredDate ?? request.SnakePreferredDate;
        if (patientId is null || patientId == Guid.Empty || wardId is null || wardId == Guid.Empty ||
            date is null || date == default(DateOnly) ||
            (request.PatientId.HasValue && request.SnakePatientId.HasValue && request.PatientId != request.SnakePatientId) ||
            (request.WardId.HasValue && request.SnakeWardId.HasValue && request.WardId != request.SnakeWardId) ||
            (request.PreferredDate.HasValue && request.SnakePreferredDate.HasValue && request.PreferredDate != request.SnakePreferredDate))
            return BadRequest(new { message = "Patient, ward and preferred date are required and must be unambiguous." });

        var created = await wards.RequestAdmissionAsync(new(patientId.Value, wardId.Value, request.Reason, date.Value, true), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { admissionRequestId = created.Id });
    }

    [HttpGet("api/treatments/{id:guid}/availability")]
    public async Task<ActionResult<TreatmentAvailabilityDto>> TreatmentAvailability(Guid id, [FromQuery, Required] DateOnly? date, CancellationToken cancellationToken) =>
        Ok(await treatments.GetAsync(id, date!.Value, cancellationToken));
}

// Accept the Python wire contract and snake_case callers without changing public JSON conventions.
public sealed class InternalAdmissionRequest
{
    public Guid? PatientId { get; init; }
    public Guid? WardId { get; init; }
    public DateOnly? PreferredDate { get; init; }
    [JsonPropertyName("patient_id")] public Guid? SnakePatientId { get; init; }
    [JsonPropertyName("ward_id")] public Guid? SnakeWardId { get; init; }
    [JsonPropertyName("preferred_date")] public DateOnly? SnakePreferredDate { get; init; }
    [Required] public string Reason { get; init; } = string.Empty;
}
