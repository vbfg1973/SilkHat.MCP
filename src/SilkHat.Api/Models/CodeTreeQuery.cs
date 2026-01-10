using SilkHat.Code.Core.Dtos;

namespace SilkHat.Api.Models;

public sealed class CodeTreeQuery
{
    public string? ParentId { get; init; }
    public int? PageNumber { get; init; }
    public int? PageSize { get; init; }
    public CodeTreeAnnotationKind? AnnotationKind { get; init; }
    public CodeTreeAnnotationKind? FilterMetric { get; init; }
    public CodeTreeFilterOperator? FilterOperator { get; init; }
    public int? FilterThreshold { get; init; }
}
