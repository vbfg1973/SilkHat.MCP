using SilkHat.Api.Models;
using SilkHat.Api.Validation;

namespace SilkHat.Api.Tests.Validation;

public sealed class CodeFileQueryValidatorTests
{
    [Fact]
    public void Validate_AllowsNonEmptyPath()
    {
        var validator = new CodeFileQueryValidator();

        var result = validator.Validate(new CodeFileQuery { Path = "Repo/Program.cs" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyPath()
    {
        var validator = new CodeFileQueryValidator();

        var result = validator.Validate(new CodeFileQuery { Path = " " });

        Assert.False(result.IsValid);
    }
}
