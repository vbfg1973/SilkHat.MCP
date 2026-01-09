using FluentValidation;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Validation;

public sealed class MethodImplementationDecisionRequestValidator : AbstractValidator<MethodImplementationDecisionRequestDto>
{
    public MethodImplementationDecisionRequestValidator()
    {
        RuleFor(request => request.InterfaceTypeName)
            .NotEmpty()
            .WithMessage("Interface type name cannot be empty.");
        RuleFor(request => request.InterfaceMethodSignature)
            .NotEmpty()
            .WithMessage("Interface method signature cannot be empty.");
        RuleFor(request => request.ImplementationTypeName)
            .NotEmpty()
            .WithMessage("Implementation type name cannot be empty.");
    }
}
