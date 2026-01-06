using SilkHat.Api.Models;
using SilkHat.Api.Validation;

namespace SilkHat.Api.Tests.Validation;

public sealed class CodeTreeQueryValidatorTests
{
    [Fact]
    public void Validate_AllowsNullParentId()
    {
        var validator = new CodeTreeQueryValidator();

        var result = validator.Validate(new CodeTreeQuery { ParentId = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyParentId()
    {
        var validator = new CodeTreeQueryValidator();

        var result = validator.Validate(new CodeTreeQuery { ParentId = " " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("ParentId", StringComparison.OrdinalIgnoreCase));
    }
}
