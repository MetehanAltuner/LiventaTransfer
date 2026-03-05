using FluentValidation;
using LiventaTransfer.Application.DTOs.Branch;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200)
            .WithMessage("Şube adı zorunludur (max 200 karakter).");
        RuleFor(x => x.Address).MaximumLength(500);
    }
}

public sealed class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200)
            .WithMessage("Şube adı zorunludur (max 200 karakter).");
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
