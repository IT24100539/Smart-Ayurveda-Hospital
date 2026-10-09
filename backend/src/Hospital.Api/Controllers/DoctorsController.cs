using FluentValidation;
using FluentValidation.Results;
using Hospital.Application.Common;
using Hospital.Application.Doctors;
using Hospital.Application.Doctors.Dtos;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorsController : ControllerBase
{
    private const string Readers = "Patient,Admin";
    private const string Managers = nameof(UserRole.Admin);

    private readonly IDoctorService _doctors;
    private readonly IValidator<CreateDoctorRequest> _createValidator;
    private readonly IValidator<UpdateDoctorRequest> _updateValidator;

    public DoctorsController(
        IDoctorService doctors,
        IValidator<CreateDoctorRequest> createValidator,
        IValidator<UpdateDoctorRequest> updateValidator)
    {
        _doctors = doctors;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = Readers)]
    [ProducesResponseType(typeof(PagedResult<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<DoctorDto>>> List(
        [FromQuery] string? query,
        [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        QueryLimits.EnsureLength("query", query);
        return Ok(await _doctors.ListAsync(query, activeOnly, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Readers)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _doctors.GetAsync(id, cancellationToken));

    [HttpGet("{id:guid}/photo")]
    [Authorize(Roles = Readers)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Photo(Guid id, CancellationToken cancellationToken)
    {
        var photo = await _doctors.OpenPhotoAsync(id, cancellationToken);
        Response.Headers.CacheControl = "private, no-store";
        var extension = photo.ContentType switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };
        Response.Headers.ContentDisposition = $"inline; filename=\"physician-photo.{extension}\"";
        return File(photo.Content, photo.ContentType);
    }

    [HttpPost]
    [Authorize(Roles = Managers)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DoctorDto>> Create(
        [FromBody] CreateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _doctors.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Managers)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> Update(
        Guid id,
        [FromBody] UpdateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _doctors.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Roles = Managers)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await _doctors.DeactivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/photo")]
    [Authorize(Roles = Managers)]
    [RequestSizeLimit(1_048_576)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_048_576)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> UploadPhoto(
        Guid id,
        // IFormFile already binds from the multipart form. [FromForm] makes Swashbuckle fail the whole document.
        IFormFile? photo,
        CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("photo", "A photo file is required.")
            });
        }

        await using var stream = photo.OpenReadStream();
        var updated = await _doctors.SavePhotoAsync(
            id,
            new DoctorPhotoContent { Content = stream, ContentType = photo.ContentType },
            cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}/photo")]
    [Authorize(Roles = Managers)]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> RemovePhoto(Guid id, CancellationToken cancellationToken) =>
        Ok(await _doctors.RemovePhotoAsync(id, cancellationToken));
}
