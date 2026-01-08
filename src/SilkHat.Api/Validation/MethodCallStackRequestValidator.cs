using FluentValidation;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Validation;

public sealed class MethodCallStackRequestValidator : AbstractValidator<MethodCallStackRequestDto>
{
    public MethodCallStackRequestValidator()
    {
        RuleFor(request => request)
            .Must(request =>
                !string.IsNullOrWhiteSpace(request.DocumentationId)
                || !string.IsNullOrWhiteSpace(request.SymbolKey))
            .WithMessage("DocumentationId or SymbolKey must be provided.");
        RuleFor(request => request.SymbolKey)
            .NotEmpty()
            .When(request => string.IsNullOrWhiteSpace(request.DocumentationId))
            .WithMessage("SymbolKey cannot be empty when DocumentationId is not provided.");
        RuleFor(request => request.MaxDepth)
            .GreaterThanOrEqualTo(0)
            .When(request => request.MaxDepth.HasValue)
            .WithMessage("MaxDepth must be zero or greater.");
    }
}
