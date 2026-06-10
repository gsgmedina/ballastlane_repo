using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IClock clock)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskItem.MaxTitleLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength);

        RuleFor(x => x.DueDateUtc!.Value)
            .GreaterThanOrEqualTo(_ => clock.UtcNow.Date)
            .When(x => x.DueDateUtc.HasValue)
            .OverridePropertyName("DueDateUtc")
            .WithMessage("Due date cannot be in the past.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue);
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator(IClock clock)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskItem.MaxTitleLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.MaxDescriptionLength);

        RuleFor(x => x.DueDateUtc!.Value)
            .GreaterThanOrEqualTo(_ => clock.UtcNow.Date)
            .When(x => x.DueDateUtc.HasValue)
            .OverridePropertyName("DueDateUtc")
            .WithMessage("Due date cannot be in the past.");

        RuleFor(x => x.Status).IsInEnum();
    }
}
