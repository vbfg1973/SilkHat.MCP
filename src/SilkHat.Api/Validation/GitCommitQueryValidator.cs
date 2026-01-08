using FluentValidation;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Validation;

public sealed class GitCommitQueryValidator : AbstractValidator<GitCommitQuery>
{
    public GitCommitQueryValidator()
    {
        RuleFor(query => query.Sha)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("Sha cannot be empty.");

        RuleFor(query => query.Path)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("Path cannot be empty.");

        RuleFor(query => query.PageNumber)
            .Must(value => value is null || value >= 1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(query => query.PageSize)
            .Must(value => value is null || value >= 1)
            .WithMessage("PageSize must be at least 1.")
            .Must(value => value is null || value <= PagingDefaults.MaxPageSize)
            .WithMessage($"PageSize must be at most {PagingDefaults.MaxPageSize}.");
    }
}
