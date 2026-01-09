using SilkHat.Code.Analysis.Models;

namespace SilkHat.Code.Analysis.Abstractions;

public interface IMethodCallStackMermaidService
{
    string BuildDiagram(IReadOnlyList<MethodCallStackNode> nodes);
}
