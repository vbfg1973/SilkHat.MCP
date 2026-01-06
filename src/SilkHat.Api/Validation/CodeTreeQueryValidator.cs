using FluentValidation;
using SilkHat.Api.Models;

namespace SilkHat.Api.Validation;

public sealed class CodeTreeQueryValidator : AbstractValidator<CodeTreeQuery>
{
    public CodeTreeQueryValidator()
    {
        RuleFor(query => query.ParentId)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("ParentId cannot be empty.");
    }
}
