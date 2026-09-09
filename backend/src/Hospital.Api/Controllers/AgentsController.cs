using FluentValidation;
using Hospital.Application.Agents;
using Hospital.Application.Agents.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Doctor")]
[Route("api/agents")]
public sealed class AgentsController : ControllerBase
{
    private readonly IAgentClient _agents;
    private readonly IValidator<AgentInvokeRequest> _validator;

    public AgentsController(IAgentClient agents, IValidator<AgentInvokeRequest> validator)
    {
        _agents = agents;
        _validator = validator;
    }

    [HttpPost("invoke")]
    public async Task<ActionResult<AgentInvokeResponse>> InvokeAgent(
        AgentInvokeRequest request,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _agents.InvokeAsync(request, cancellationToken));
    }
}
