using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class SymbolKeyUtilityTests
{
    [Fact]
    public void GetSymbolKeyString_ReturnsNonEmptyKey()
    {
        var tree = CSharpSyntaxTree.ParseText("""
namespace Sample;

public class Foo
{
}
""");

        var compilation = CSharpCompilation.Create(
            "SampleAssembly",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var symbol = compilation.GetTypeByMetadataName("Sample.Foo");
        Assert.NotNull(symbol);

        var key = SymbolKeyUtility.GetSymbolKeyString(symbol!, compilation);

        Assert.False(string.IsNullOrWhiteSpace(key));
    }
}
