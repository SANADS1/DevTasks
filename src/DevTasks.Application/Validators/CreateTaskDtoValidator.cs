using FluentValidation;
using DevTasks.Application.DTOs;

namespace DevTasks.Application.Validators
{
    public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
    {
        public CreateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            //RuleFor(x => x.DueDate)
            //    .Must(date => date == null || date.Value.Date >= DateTime.UtcNow.Date)
            //    .WithMessage("Due date cannot be in the past.");
        }
    }
}