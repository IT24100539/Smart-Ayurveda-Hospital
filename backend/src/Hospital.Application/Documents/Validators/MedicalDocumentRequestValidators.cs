using FluentValidation;
using Hospital.Application.Documents.Dtos;
using Hospital.Domain.Entities;

namespace Hospital.Application.Documents.Validators;

public sealed class UploadMedicalDocumentRequestValidator : AbstractValidator<UploadMedicalDocumentRequest>
{
    public UploadMedicalDocumentRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(MedicalDocument.TitleMaxLength);
        RuleFor(x => x.Summary).MaximumLength(MedicalDocument.SummaryMaxLength);
        RuleFor(x => x.Category).IsInEnum();
    }
}
