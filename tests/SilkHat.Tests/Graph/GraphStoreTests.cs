using SilkHat.Code.Analysis.Graph;

namespace SilkHat.Tests.Graph;

public sealed class GraphStoreTests
{
    [Fact]
    public void AddNode_And_FilteredEdges_Work()
    {
        var store = new GraphStore();
        var project = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Project, "proj");
        var file = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.File, "file");
        var method = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Method, "method");

        Assert.True(store.AddNode(project));
        Assert.True(store.AddNode(file));
        Assert.True(store.AddNode(method));

        Assert.True(store.AddEdge(project.Id, file.Id, EdgeType.Contains));
        Assert.True(store.AddEdge(file.Id, method.Id, EdgeType.DeclaresMember));

        Assert.Equal(1, store.GetOutDegree(project.Id, EdgeType.Contains));
        Assert.Equal(0, store.GetOutDegree(project.Id, EdgeType.DeclaresMember));

        var memberEdges = store.GetOutEdges(file.Id, EdgeType.DeclaresMember).ToList();
        Assert.Single(memberEdges);
        Assert.Equal(method.Id, memberEdges[0].TargetId);
    }

    [Fact]
    public void ParameterEdges_PreserveOrdinal()
    {
        var store = new GraphStore();
        var method = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Method, "method");
        var parameter = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Parameter, "param");
        var paramType = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.NamedType, "System.String");

        store.AddNode(method);
        store.AddNode(parameter);
        store.AddNode(paramType);

        var attributes = new Dictionary<string, string> { { "ordinal", "1" } };
        Assert.True(store.AddEdge(method.Id, parameter.Id, EdgeType.Parameter, attributes));
        Assert.True(store.AddEdge(parameter.Id, paramType.Id, EdgeType.ParameterType));

        var paramEdges = store.GetOutEdges(method.Id, EdgeType.Parameter).ToList();
        Assert.Single(paramEdges);
        string? value = null;
        var hasOrdinal = paramEdges[0].Attributes is { } attrs &&
                         attrs.TryGetValue("ordinal", out value);
        Assert.True(hasOrdinal);
        Assert.NotNull(value);
        Assert.Equal("1", value);
    }

    [Fact]
    public void MethodCallEdges_AreTyped()
    {
        var store = new GraphStore();
        var caller = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Method, "Caller");
        var callee = new GraphNodeDto(Guid.NewGuid(), GraphNodeKind.Method, "Callee");
        store.AddNode(caller);
        store.AddNode(callee);

        Assert.True(store.AddEdge(caller.Id, callee.Id, EdgeType.MethodCall));

        var calls = store.GetOutEdges(caller.Id, EdgeType.MethodCall).ToList();
        Assert.Single(calls);
        Assert.Equal(callee.Id, calls[0].TargetId);
    }
}
