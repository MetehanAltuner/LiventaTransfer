using FluentValidation;
using LiventaTransfer.Application.DTOs.Customer;
using LiventaTransfer.Domain.Enums;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
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
            .NotEmpty().WithMessage("Kurumsal müşteriler için vergi numarası zorunludur.")
            .When(x => x.CustomerType == CustomerType.Corporate);

        RuleFor(x => x.TcKimlikNo)
            .NotEmpty().WithMessage("Bireysel müşteriler için TC Kimlik No zorunludur.")
            .Length(11).WithMessage("TC Kimlik No 11 haneli olmalıdır.")
            .Matches(@"^\d{11}$").WithMessage("TC Kimlik No sadece rakam içermelidir.")
            .When(x => x.CustomerType == CustomerType.Individual);
    }
}

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
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
            .NotEmpty().WithMessage("Kurumsal müşteriler için vergi numarası zorunludur.")
            .When(x => x.CustomerType == CustomerType.Corporate);

        RuleFor(x => x.TcKimlikNo)
            .NotEmpty().WithMessage("Bireysel müşteriler için TC Kimlik No zorunludur.")
            .Length(11).WithMessage("TC Kimlik No 11 haneli olmalıdır.")
            .Matches(@"^\d{11}$").WithMessage("TC Kimlik No sadece rakam içermelidir.")
            .When(x => x.CustomerType == CustomerType.Individual);
    }
}
