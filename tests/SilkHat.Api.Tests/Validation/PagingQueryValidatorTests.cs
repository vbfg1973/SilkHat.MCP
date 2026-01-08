using SilkHat.Api.Models;
using SilkHat.Api.Validation;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Tests.Validation;

public sealed class PagingQueryValidatorTests
{
    [Fact]
    public void Validate_AllowsNulls()
    {
        var validator = new PagingQueryValidator();

        var result = validator.Validate(new PagingQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsInvalidValues()
    {
        var validator = new PagingQueryValidator();

        var result = validator.Validate(new PagingQuery { PageNumber = 0, PageSize = PagingDefaults.MaxPageSize + 1 });

        Assert.False(result.IsValid);
    }
}
