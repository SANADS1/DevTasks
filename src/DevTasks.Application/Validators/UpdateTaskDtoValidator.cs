using FluentValidation;
using DevTasks.Application.DTOs;

namespace DevTasks.Application.Validators
{
    public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
    {
        public UpdateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            // IsCompleted is a bool — it can only ever be true or false,
            // so there's nothing to validate there. Included here just to
            // show that not every property needs a rule.
        }
    }
}