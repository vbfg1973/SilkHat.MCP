using SilkHat.Api.Models;
using SilkHat.Api.Validation;
using SilkHat.Code.Core.Dtos;
using SilkHat.Core.Dtos;

namespace SilkHat.Api.Tests.Validation
{
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
            Assert.Contains(result.Errors,
                error => error.ErrorMessage.Contains("ParentId", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Validate_RejectsInvalidPaging()
        {
            var validator = new CodeTreeQueryValidator();

            var result = validator.Validate(new CodeTreeQuery
                { PageNumber = 0, PageSize = PagingDefaults.MaxPageSize + 1 });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_RejectsMissingFilterFields()
        {
            var validator = new CodeTreeQueryValidator();

            var result = validator.Validate(new CodeTreeQuery
            {
                FilterMetric = CodeTreeAnnotationKind.CyclomaticComplexity
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors,
                error => error.ErrorMessage.Contains("Filter", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Validate_AllowsCompleteFilter()
        {
            var validator = new CodeTreeQueryValidator();

            var result = validator.Validate(new CodeTreeQuery
            {
                FilterMetric = CodeTreeAnnotationKind.CyclomaticComplexity,
                FilterOperator = CodeTreeFilterOperator.GreaterThan,
                FilterThreshold = 5
            });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_RejectsNegativeThreshold()
        {
            var validator = new CodeTreeQueryValidator();

            var result = validator.Validate(new CodeTreeQuery
            {
                FilterMetric = CodeTreeAnnotationKind.CognitiveComplexity,
                FilterOperator = CodeTreeFilterOperator.LessThanOrEqual,
                FilterThreshold = -1
            });

            Assert.False(result.IsValid);
        }
    }
}