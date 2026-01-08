using FluentValidation;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Validation;

public sealed class MethodCallStackRequestValidator : AbstractValidator<MethodCallStackRequestDto>
{
    public MethodCallStackRequestValidator()
    {
        RuleFor(request => request.SymbolKey)
            .NotEmpty()
            .WithMessage("SymbolKey cannot be empty.");
        RuleFor(request => request.MaxDepth)
            .GreaterThanOrEqualTo(0)
            .When(request => request.MaxDepth.HasValue)
            .WithMessage("MaxDepth must be zero or greater.");
    }
}
