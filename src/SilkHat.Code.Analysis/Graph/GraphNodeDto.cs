using System.Text.Json.Serialization;

namespace SilkHat.Code.Analysis.Graph
{
    public sealed record GraphNodeDto(
        Guid Id,
        GraphNodeKind Kind,
        string Key,
        string? Label = null,
        IReadOnlyDictionary<string, string>? Attributes = null)
    {
        [JsonIgnore] public bool IsEmpty => Id == Guid.Empty || string.IsNullOrWhiteSpace(Key);
    }
}