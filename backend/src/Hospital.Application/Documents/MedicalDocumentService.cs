using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Application.Documents.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Documents;

public sealed class MedicalDocumentService : IMedicalDocumentService
{
    private readonly IMedicalDocumentRepository _documents;
    private readonly IMedicalDocumentStore _files;
    private readonly IPatientRepository _patients;
    private readonly IUserRepository _users;
    private readonly IActorContext _actors;
    private readonly ICurrentUser _current;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MedicalDocumentService(
        IMedicalDocumentRepository documents,
        IMedicalDocumentStore files,
        IPatientRepository patients,
        IUserRepository users,
        IActorContext actors,
        ICurrentUser current,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _documents = documents;
        _files = files;
        _patients = patients;
        _users = users;
        _actors = actors;
        _current = current;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PagedResult<MedicalDocumentDto>> ListForPatientAsync(
        Guid patientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureStaff("open");
        _ = await _patients.GetByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException("Patient", patientId);
        var (items, total) = await _documents.ListByPatientAsync(patientId, page, pageSize, cancellationToken);
        return Page(items, total, page, pageSize);
    }

    public async Task<PagedResult<MedicalDocumentDto>> ListMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (_current.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only a patient can read their own medical documents.");
        }

        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var (items, total) = await _documents.ListByPatientAsync(patient.Id, page, pageSize, cancellationToken);
        return Page(items, total, page, pageSize);
    }

    public async Task<MedicalDocumentDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await RequireReadableAsync(id, cancellationToken);
        return Map(document);
    }

    public async Task<MedicalDocumentFile> OpenAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await RequireReadableAsync(id, cancellationToken);
        if (!IsAllowedContentType(document.ContentType))
        {
            throw new NotFoundException("Medical document", id);
        }

        var stream = _files.OpenRead(document.FilePath)
            ?? throw new NotFoundException("Medical document", id);
        return new MedicalDocumentFile
        {
            Content = stream,
            ContentType = document.ContentType,
            DownloadName = DownloadName(document.ContentType)
        };
    }

    public async Task<MedicalDocumentDto> UploadAsync(
        UploadMedicalDocumentRequest request,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var staff = await RequireStaffUserAsync(cancellationToken);
        var title = request.Title.Trim();
        if (title.Length == 0 || title.Length > MedicalDocument.TitleMaxLength)
        {
            throw new DomainException("The document title is invalid.");
        }

        var summary = request.Summary?.Trim() ?? string.Empty;
        if (summary.Length > MedicalDocument.SummaryMaxLength)
        {
            throw new DomainException("The document summary is too long.");
        }

        if (!Enum.IsDefined(request.Category))
        {
            throw new DomainException("The document category is invalid.");
        }

        _ = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException("Patient", request.PatientId);

        var saved = await _files.SaveAsync(content, contentType, cancellationToken);
        var document = new MedicalDocument
        {
            PatientId = request.PatientId,
            Title = title,
            Category = request.Category,
            FilePath = saved.StorageKey,
            ContentType = saved.ContentType,
            FileSizeBytes = saved.SizeBytes,
            UploadedAt = _clock.UtcNow,
            UploadedByUserId = staff.Id,
            Summary = summary
        };

        try
        {
            await _documents.AddAsync(document, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _files.DeleteAsync(saved.StorageKey, cancellationToken);
            throw;
        }

        return Map(document);
    }

    private async Task<MedicalDocument> RequireReadableAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        var document = await _documents.GetAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException("Medical document", id);

        if (_current.Role == UserRole.Patient)
        {
            var patient = await _actors.RequirePatientAsync(cancellationToken);
            if (patient.Id != document.PatientId)
            {
                throw new ForbiddenException("You can only read your own medical documents.");
            }

            return document;
        }

        if (!IsStaff(_current.Role))
        {
            throw new ForbiddenException("Your role cannot read medical documents.");
        }

        return document;
    }

    private async Task<User> RequireStaffUserAsync(CancellationToken cancellationToken)
    {
        EnsureStaff("upload");
        var user = await _users.GetByIdAsync(_current.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Account was not found.");
        if (!user.IsActive || !IsStaff(user.Role))
        {
            throw new ForbiddenException("Only hospital staff can upload medical documents.");
        }

        return user;
    }

    private void EnsureStaff(string action)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        if (!IsStaff(_current.Role))
        {
            throw new ForbiddenException($"Only hospital staff can {action} medical documents.");
        }
    }

    private static bool IsStaff(UserRole role) =>
        role is UserRole.FrontDeskStaff or UserRole.Doctor or UserRole.Admin or UserRole.Therapist;

    private static bool IsAllowedContentType(string? contentType) =>
        contentType is "application/pdf" or "image/jpeg" or "image/png" or "image/webp" or "image/gif";

    private static string DownloadName(string contentType) => contentType switch
    {
        "application/pdf" => "document.pdf",
        "image/png" => "document.png",
        "image/webp" => "document.webp",
        "image/gif" => "document.gif",
        _ => "document.jpg"
    };

    private static PagedResult<MedicalDocumentDto> Page(
        IReadOnlyList<MedicalDocument> items,
        int total,
        int page,
        int pageSize) =>
        new()
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

    private static MedicalDocumentDto Map(MedicalDocument document) =>
        new(
            document.Id,
            document.PatientId,
            document.Title,
            document.Category,
            document.ContentType,
            document.FileSizeBytes,
            document.UploadedAt,
            $"/api/medical-documents/{document.Id}/file",
            document.Summary);
}
