using FluentValidation;
using LiventaTransfer.Application.DTOs.Auth;

namespace LiventaTransfer.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Kullanıcı adı zorunludur.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre zorunludur.");
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(100)
            .WithMessage("Kullanıcı adı en az 3, en fazla 100 karakter olmalıdır.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
            .WithMessage("Şifre en az 6 karakter olmalıdır.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100)
            .WithMessage("Ad zorunludur.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100)
            .WithMessage("Soyad zorunludur.");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Geçersiz rol.");
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("Şube zorunludur.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Mevcut şifre zorunludur.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6)
            .WithMessage("Yeni şifre en az 6 karakter olmalıdır.");
    }
}
