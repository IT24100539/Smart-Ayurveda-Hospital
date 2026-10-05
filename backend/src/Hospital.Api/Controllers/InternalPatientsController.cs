using Hospital.Api.Authentication;
using Hospital.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Policy = InternalServiceAuthenticationHandler.PolicyName)]
[Route("api/internal/patients")]
public sealed class InternalPatientsController : ControllerBase
{
    private readonly IPatientRepository _patients;

    public InternalPatientsController(IPatientRepository patients) => _patients = patients;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(id, cancellationToken);
        if (patient is null) return NotFound();

        return Ok(new
        {
            id = patient.Id,
            uhid = patient.Uhid,
            firstName = patient.FirstName,
            lastName = patient.LastName,
            fullName = $"{patient.FirstName} {patient.LastName}",
            dateOfBirth = patient.DateOfBirth,
            gender = patient.Gender.ToString(),
            phone = patient.Phone,
            email = patient.Email,
            address = patient.Address,
            bloodGroup = patient.BloodGroup,
            allergies = patient.Allergies,
            prakriti = patient.Prakriti.ToString(),
            vikriti = patient.Vikriti.ToString(),
            isActive = patient.IsActive,
            registeredAt = patient.CreatedAt
        });
    }
}
