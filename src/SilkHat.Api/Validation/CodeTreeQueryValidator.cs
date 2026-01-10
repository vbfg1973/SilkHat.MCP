using FluentValidation;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Validation;

public sealed class CodeTreeQueryValidator : AbstractValidator<CodeTreeQuery>
{
    public CodeTreeQueryValidator()
    {
        RuleFor(query => query.ParentId)
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("ParentId cannot be empty.");

        RuleFor(query => query.PageNumber)
            .Must(value => value is null || value >= 1)
            .WithMessage("PageNumber must be at least 1.");

        RuleFor(query => query.PageSize)
            .Must(value => value is null || value >= 1)
            .WithMessage("PageSize must be at least 1.")
            .Must(value => value is null || value <= PagingDefaults.MaxPageSize)
            .WithMessage($"PageSize must be at most {PagingDefaults.MaxPageSize}.");

        When(HasAnyFilterField, () =>
        {
            RuleFor(query => query.FilterMetric)
                .NotNull()
                .WithMessage("FilterMetric is required when filtering.");
            RuleFor(query => query.FilterOperator)
                .NotNull()
                .WithMessage("FilterOperator is required when filtering.");
            RuleFor(query => query.FilterThreshold)
                .NotNull()
                .WithMessage("FilterThreshold is required when filtering.")
                .Must(value => value is null || value >= 0)
                .WithMessage("FilterThreshold must be at least 0.");
        });
    }

    private static bool HasAnyFilterField(CodeTreeQuery query)
    {
        return query.FilterMetric is not null
            || query.FilterOperator is not null
            || query.FilterThreshold is not null;
    }
}
