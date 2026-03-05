using FluentValidation;
using LiventaTransfer.Application.DTOs.Notification;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.RecipientType).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().WithMessage("Mesaj zorunludur.").MaximumLength(2000);
        RuleFor(x => x.RecipientPhone).MaximumLength(20);
    }
}
