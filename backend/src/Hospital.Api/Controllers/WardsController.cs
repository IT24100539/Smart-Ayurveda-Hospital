using Hospital.Application.Wards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/wards")]
public sealed class WardsController : ControllerBase
{
    private readonly IWardService _wards;

    public WardsController(IWardService wards) => _wards = wards;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WardOccupancyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _wards.GetAllOccupancyAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<WardOccupancyDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        // staff sees bed-level detail
        var dto = await _wards.GetOccupancyAsync(id, forPatient: false, cancellationToken);
        if (dto == null) return NotFound();
        return Ok(dto);
    }
}
