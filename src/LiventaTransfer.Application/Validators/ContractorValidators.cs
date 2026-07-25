using FluentValidation;
using LiventaTransfer.Application.DTOs.Contractor;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateContractorRequestValidator : AbstractValidator<CreateContractorRequest>
{
    public CreateContractorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CustomerType).IsInEnum();
        RuleFor(x => x.TaxNumber).MaximumLength(20);
        RuleFor(x => x.TaxOffice).MaximumLength(200);
        RuleFor(x => x.TcKimlikNo).MaximumLength(11);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Kurumsal yükleniciler için vergi numarası zorunludur.")
            .When(x => x.CustomerType == CustomerType.Corporate);

        // TC Kimlik No zorunlu değil; ancak girilirse 11 haneli ve sadece rakam olmalı.
        RuleFor(x => x.TcKimlikNo)
            .Length(11).WithMessage("TC Kimlik No 11 haneli olmalıdır.")
            .Matches(@"^\d{11}$").WithMessage("TC Kimlik No sadece rakam içermelidir.")
            .When(x => !string.IsNullOrWhiteSpace(x.TcKimlikNo));
    }
}

public sealed class UpdateContractorRequestValidator : AbstractValidator<UpdateContractorRequest>
{
    public UpdateContractorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CustomerType).IsInEnum();
        RuleFor(x => x.TaxNumber).MaximumLength(20);
        RuleFor(x => x.TaxOffice).MaximumLength(200);
        RuleFor(x => x.TcKimlikNo).MaximumLength(11);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Kurumsal yükleniciler için vergi numarası zorunludur.")
            .When(x => x.CustomerType == CustomerType.Corporate);

        // TC Kimlik No zorunlu değil; ancak girilirse 11 haneli ve sadece rakam olmalı.
        RuleFor(x => x.TcKimlikNo)
            .Length(11).WithMessage("TC Kimlik No 11 haneli olmalıdır.")
            .Matches(@"^\d{11}$").WithMessage("TC Kimlik No sadece rakam içermelidir.")
            .When(x => !string.IsNullOrWhiteSpace(x.TcKimlikNo));
    }
}
