using SilkHat.Api.Models;
using SilkHat.Api.Validation;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Tests.Validation
{
    public sealed class GitCommitQueryValidatorTests
    {
        [Fact]
        public void Validate_AllowsNulls()
        {
            var validator = new GitCommitQueryValidator();

            var result = validator.Validate(new GitCommitQuery());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_RejectsEmptySha()
        {
            var validator = new GitCommitQueryValidator();

            var result = validator.Validate(new GitCommitQuery { Sha = " " });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_RejectsEmptyPath()
        {
            var validator = new GitCommitQueryValidator();

            var result = validator.Validate(new GitCommitQuery { Path = "" });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_RejectsInvalidPaging()
        {
            var validator = new GitCommitQueryValidator();

            var result = validator.Validate(new GitCommitQuery
                { PageNumber = 0, PageSize = PagingDefaults.MaxPageSize + 1 });

            Assert.False(result.IsValid);
        }
    }
}