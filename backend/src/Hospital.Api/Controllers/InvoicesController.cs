using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Application.Billing;
using Hospital.Application.Billing.Dtos;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

/// <summary>
/// Bills for a treatment visit or an approved ward admission.
/// Front desk records the payment by hand. There is no payment gateway.
/// Patients can read only their own issued or paid invoices.
/// </summary>
[ApiController]
[Authorize]
[Route("api/invoices")]
public sealed class InvoicesController : ControllerBase
{
    private const string BillingStaff = "FrontDeskStaff,Admin";
    private const string Readers = "FrontDeskStaff,Admin,Doctor";

    private readonly IInvoiceService _invoices;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreateInvoiceRequest> _createValidator;
    private readonly IValidator<UpdateInvoiceRequest> _updateValidator;
    private readonly IValidator<RecordPaymentRequest> _paymentValidator;

    public InvoicesController(
        IInvoiceService invoices,
        IAuditLogService audit,
        IValidator<CreateInvoiceRequest> createValidator,
        IValidator<UpdateInvoiceRequest> updateValidator,
        IValidator<RecordPaymentRequest> paymentValidator)
    {
        _invoices = invoices;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _paymentValidator = paymentValidator;
    }

    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Patient))]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _invoices.ListMineAsync(page, pageSize, cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = Readers)]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> List(
        [FromQuery] Guid? patientId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _invoices.ListAsync(patientId, status, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetAsync(id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.Invoice, id.ToString(), cancellationToken);
        return Ok(invoice);
    }

    [HttpPost]
    [Authorize(Roles = BillingStaff)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _invoices.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = BillingStaff)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Update(
        Guid id,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _invoices.UpdateDraftAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = BillingStaff)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Issue(Guid id, CancellationToken cancellationToken) =>
        Ok(await _invoices.IssueAsync(id, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = BillingStaff)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await _invoices.CancelAsync(id, cancellationToken));

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = BillingStaff)]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(
        Guid id,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        await _paymentValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _invoices.RecordPaymentAsync(id, request, cancellationToken));
    }
}
