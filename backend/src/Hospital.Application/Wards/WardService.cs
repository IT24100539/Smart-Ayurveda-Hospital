using Hospital.Application.Abstractions;
using Hospital.Application.Wards;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Wards;

public sealed class WardService : IWardService
{
    private readonly IWardRepository _wards;
    private readonly IPatientRepository _patients;
    private readonly IUnitOfWork _uow;

    public WardService(IWardRepository wards, IPatientRepository patients, IUnitOfWork uow)
    {
        _wards = wards;
        _patients = patients;
        _uow = uow;
    }

    public async Task<IEnumerable<WardOccupancyDto>> GetAllOccupancyAsync(CancellationToken cancellationToken)
    {
        var wards = await _wards.ListAllAsync(cancellationToken);
        return wards.Select(w => new WardOccupancyDto(
            w.Id,
            w.Name,
            w.NameSinhala,
            w.Gender,
            w.TotalCapacity,
            w.Beds.Count(b => b.IsOccupied),
            w.Beds.Select(b => new BedDto(b.Id, b.BedLabel, b.IsOccupied))));
    }

    public async Task<WardOccupancyDto?> GetOccupancyAsync(Guid id, bool forPatient, CancellationToken cancellationToken)
    {
        var ward = await _wards.GetWithBedsAsync(id, cancellationToken);
        if (ward == null) return null;
        var beds = ward.Beds.Select(b => new BedDto(b.Id, b.BedLabel, b.IsOccupied));
        if (forPatient)
        {
            // For patient, do not expose bed labels tied to other patients: only provide aggregate and anonymized bed entries
            return new WardOccupancyDto(ward.Id, ward.Name, ward.NameSinhala, ward.Gender, ward.TotalCapacity, ward.Beds.Count(b => b.IsOccupied), Enumerable.Empty<BedDto>());
        }

        return new WardOccupancyDto(ward.Id, ward.Name, ward.NameSinhala, ward.Gender, ward.TotalCapacity, ward.Beds.Count(b => b.IsOccupied), beds);
    }

    public async Task<AdmissionRequestDto> RequestAdmissionAsync(CreateAdmissionRequestRequest request, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken) ?? throw new InvalidOperationException("Patient not found");
        var admission = new AdmissionRequest
        {
            PatientId = patient.Id,
            WardId = request.WardId,
            Reason = request.Reason,
            PreferredDate = request.PreferredDate,
            RequestedByAgent = request.RequestedByAgent,
            Status = AdmissionRequestStatus.Pending
        };
        await _wards.AddAdmissionRequestAsync(admission, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Map(admission, patient);
    }

    public async Task<IReadOnlyList<AdmissionRequestDto>> ListPendingAdmissionsAsync(CancellationToken cancellationToken)
    {
        var list = await _wards.ListPendingAdmissionsAsync(cancellationToken);
        return list.Select(ar => Map(ar, ar.Patient)).ToList();
    }

    public async Task<bool> DecideAdmissionAsync(Guid id, AdmissionDecisionRequest request, CancellationToken cancellationToken)
    {
        var decidedAt = DateTimeOffset.UtcNow;
        if (request.Approve)
        {
            // Pre-check free beds to provide a clear WardFullException when no beds are available
            var admission = await _wards.GetAdmissionRequestByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Admission request not found");
            if (admission.WardId is null)
            {
                throw new InvalidOperationException("Admission request has no ward assigned.");
            }

            var ward = await _wards.GetWithBedsAsync(admission.WardId.Value, cancellationToken) ?? throw new InvalidOperationException("Ward not found");
            var freeBeds = ward.Beds.Count(b => !b.IsOccupied);
            if (freeBeds == 0)
            {
                throw new Hospital.Domain.Exceptions.WardFullException("No free beds available in the selected ward.");
            }

            var ok = await _wards.TryApproveAdmissionAssignBedAsync(id, request.DecidedBy, decidedAt, cancellationToken);
            if (!ok)
            {
                throw new Hospital.Domain.Exceptions.WardFullException("No free beds available in the selected ward.");
            }

            return true;
        }

        var admission2 = await _wards.GetAdmissionRequestByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Admission request not found");
        admission2.Status = AdmissionRequestStatus.Rejected;
        admission2.DecidedBy = request.DecidedBy;
        admission2.DecidedAt = decidedAt;
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AdmissionRequestDto Map(AdmissionRequest a, Patient p) => new(
        a.Id,
        a.PatientId,
        $"{p.FirstName} {p.LastName}",
        a.WardId,
        a.BedId,
        a.Reason,
        a.PreferredDate,
        a.Status.ToString(),
        a.RequestedByAgent,
        a.DecidedBy,
        a.DecidedAt);
}
