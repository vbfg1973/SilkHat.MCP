using FluentValidation;
using SilkHat.Api.Models;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Validation
{
    public sealed class PagingQueryValidator : AbstractValidator<PagingQuery>
    {
        public PagingQueryValidator()
        {
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
}