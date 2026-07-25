using FluentValidation;
using LiventaTransfer.Application.DTOs.Contractor;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateContractorRequestValidator : AbstractValidator<CreateContractorRequest>
{
    public CreateContractorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
        RuleFor(x => x.TaxOffice).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class UpdateContractorRequestValidator : AbstractValidator<UpdateContractorRequest>
{
    public UpdateContractorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
        RuleFor(x => x.TaxOffice).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
