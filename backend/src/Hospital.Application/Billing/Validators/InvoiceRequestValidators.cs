using FluentValidation;
using Hospital.Application.Billing.Dtos;
using Hospital.Domain.Entities;

namespace Hospital.Application.Billing.Validators;

public sealed class InvoiceLineRequestValidator : AbstractValidator<InvoiceLineRequest>
{
    public InvoiceLineRequestValidator()
    {
        RuleFor(line => line)
            .Must(line => line.AppointmentId.HasValue != line.AdmissionId.HasValue)
            .WithMessage("Each line must be one treatment visit or one admission.");

        When(line => line.AppointmentId.HasValue, () =>
        {
            RuleFor(line => line.AppointmentId).NotEmpty();
            RuleFor(line => line.Quantity)
                .Equal(1)
                .WithMessage("A treatment visit is billed once.");
            RuleFor(line => line.UnitPrice)
                .Null()
                .WithMessage("A treatment visit is charged at the catalog price.");
        });

        When(line => line.AdmissionId.HasValue, () =>
        {
            RuleFor(line => line.AdmissionId).NotEmpty();
            RuleFor(line => line.Quantity)
                .InclusiveBetween(1, InvoiceLine.MaxQuantity)
                .WithMessage($"An admission is billed in days, from 1 to {InvoiceLine.MaxQuantity}.");
            RuleFor(line => line.UnitPrice)
                .NotNull()
                .WithMessage("An admission needs a daily rate.")
                .GreaterThan(0)
                .LessThanOrEqualTo(InvoiceLine.MaxUnitPrice)
                .Must(price => price is null || InvoiceLine.HasAtMostTwoDecimals(price.Value))
                .WithMessage("The daily rate can have at most two decimal places.");
        });
    }
}

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(request => request.PatientId).NotEmpty();
        RuleFor(request => request.Currency)
            .Must(InvoiceCurrency.IsAcceptable)
            .WithMessage("Currency must be a three-letter code. Leave it blank to use LKR.");
        RuleFor(request => request.Notes).MaximumLength(Invoice.NotesMaxLength);
        RuleFor(request => request.Lines).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.Count <= InvoiceLine.MaxLines)
            .WithMessage($"An invoice can include at most {InvoiceLine.MaxLines} lines.");
        RuleForEach(request => request.Lines)
            .SetValidator(new InvoiceLineRequestValidator())
            .When(request => request.Lines is not null);
    }
}

public sealed class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(request => request.Currency)
            .Must(InvoiceCurrency.IsAcceptable)
            .WithMessage("Currency must be a three-letter code. Leave it blank to use LKR.");
        RuleFor(request => request.Notes).MaximumLength(Invoice.NotesMaxLength);
        RuleFor(request => request.Lines).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.Count <= InvoiceLine.MaxLines)
            .WithMessage($"An invoice can include at most {InvoiceLine.MaxLines} lines.");
        RuleForEach(request => request.Lines)
            .SetValidator(new InvoiceLineRequestValidator())
            .When(request => request.Lines is not null);
    }
}

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(InvoiceLine.MaxTotal)
            .Must(InvoiceLine.HasAtMostTwoDecimals)
            .WithMessage("The payment amount can have at most two decimal places.");
        RuleFor(request => request.Method)
            .NotEmpty()
            .Must(InvoicePaymentMethods.IsNamed)
            .WithMessage("Payment method must be Cash, Card, or BankTransfer.");
        RuleFor(request => request.PaidOn)
            .Must(paidOn => paidOn.Year is >= 1900 and <= 2100)
            .WithMessage("The payment date is outside the supported range.");
        RuleFor(request => request.Reference).MaximumLength(InvoicePayment.ReferenceMaxLength);
    }
}

internal static class InvoiceCurrency
{
    public static bool IsAcceptable(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return true;
        }

        var code = currency.Trim();
        return code.Length == Invoice.CurrencyLength && code.All(char.IsLetter);
    }
}

internal static class InvoicePaymentMethods
{
    public static bool IsNamed(string? method)
    {
        if (string.IsNullOrWhiteSpace(method) || int.TryParse(method, out _))
        {
            return false;
        }

        return Enum.TryParse<InvoicePaymentMethod>(method.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed);
    }
}
