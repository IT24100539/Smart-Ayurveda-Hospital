using FluentValidation;
using FluentValidation.Results;
using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Application.Common;
using Hospital.Application.Documents;
using Hospital.Application.Documents.Dtos;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

/// <summary>
/// Clinical files for one patient chart. Staff upload PDF and common image types.
/// The uploaded file name is ignored; the stored name is generated and kept outside the web root.
/// Patients can read only their own documents. Bytes are served from this endpoint after an ownership check.
/// </summary>
[ApiController]
[Authorize]
[Route("api/medical-documents")]
public sealed class MedicalDocumentsController : ControllerBase
{
    private const string Staff = "FrontDeskStaff,Doctor,Admin,Therapist";

    private readonly IMedicalDocumentService _documents;
    private readonly IAuditLogService _audit;
    private readonly IValidator<UploadMedicalDocumentRequest> _uploadValidator;

    public MedicalDocumentsController(
        IMedicalDocumentService documents,
        IAuditLogService audit,
        IValidator<UploadMedicalDocumentRequest> uploadValidator)
    {
        _documents = documents;
        _audit = audit;
        _uploadValidator = uploadValidator;
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(PagedResult<MedicalDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<MedicalDocumentDto>>> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _documents.ListMineAsync(page, pageSize, cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = Staff)]
    [ProducesResponseType(typeof(PagedResult<MedicalDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MedicalDocumentDto>>> List(
        [FromQuery] Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (patientId == Guid.Empty)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("patientId", "A patient is required.")
            });
        }

        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _documents.ListForPatientAsync(patientId, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MedicalDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalDocumentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documents.GetAsync(id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.ClinicalRecord, id.ToString(), cancellationToken);
        return Ok(document);
    }

    [HttpGet("{id:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var file = await _documents.OpenAsync(id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.ClinicalRecord, id.ToString(), cancellationToken);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.ContentDisposition = $"inline; filename=\"{file.DownloadName}\"";
        return File(file.Content, file.ContentType);
    }

    [HttpPost]
    [Authorize(Roles = Staff)]
    [RequestSizeLimit(MedicalDocumentOptions.RequestBytesCeiling)]
    [RequestFormLimits(MultipartBodyLengthLimit = MedicalDocumentOptions.RequestBytesCeiling)]
    [ProducesResponseType(typeof(MedicalDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalDocumentDto>> Upload(
        [FromForm] Guid patientId,
        [FromForm] string? title,
        [FromForm] DocumentCategory category,
        [FromForm] string? summary,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("file", "A document file is required.")
            });
        }

        var request = new UploadMedicalDocumentRequest(
            patientId,
            title?.Trim() ?? string.Empty,
            category,
            string.IsNullOrWhiteSpace(summary) ? null : summary.Trim());
        await _uploadValidator.ValidateAndThrowAsync(request, cancellationToken);
        await using var stream = file.OpenReadStream();
        var created = await _documents.UploadAsync(request, stream, file.ContentType, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}
