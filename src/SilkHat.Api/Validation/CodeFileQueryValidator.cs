using FluentValidation;
using SilkHat.Api.Models;

namespace SilkHat.Api.Validation;

public sealed class CodeFileQueryValidator : AbstractValidator<CodeFileQuery>
{
    public CodeFileQueryValidator()
    {
        RuleFor(query => query.Path)
            .Must(value => value is not null && !string.IsNullOrWhiteSpace(value))
            .WithMessage("Path cannot be empty.");
    }
}
