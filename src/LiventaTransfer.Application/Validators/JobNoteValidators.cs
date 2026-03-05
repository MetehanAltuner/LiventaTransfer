using FluentValidation;
using LiventaTransfer.Application.DTOs.JobNote;

namespace LiventaTransfer.Application.Validators;

public sealed class CreateJobNoteRequestValidator : AbstractValidator<CreateJobNoteRequest>
{
    public CreateJobNoteRequestValidator()
    {
        RuleFor(x => x.NoteText).NotEmpty().WithMessage("Not metni zorunludur.").MaximumLength(2000);
    }
}

public sealed class UpdateJobNoteRequestValidator : AbstractValidator<UpdateJobNoteRequest>
{
    public UpdateJobNoteRequestValidator()
    {
        RuleFor(x => x.NoteText).NotEmpty().WithMessage("Not metni zorunludur.").MaximumLength(2000);
    }
}
