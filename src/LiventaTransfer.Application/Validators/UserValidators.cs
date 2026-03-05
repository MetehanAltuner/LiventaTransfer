using FluentValidation;
using LiventaTransfer.Application.DTOs.User;

namespace LiventaTransfer.Application.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("Şube seçimi zorunludur.");
    }
}
