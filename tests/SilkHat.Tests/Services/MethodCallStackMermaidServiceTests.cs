using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services
{
    public sealed class MethodCallStackMermaidServiceTests
    {
        [Fact]
        public void BuildDiagram_ReturnsSequenceDiagramWithReturns()
        {
            var service = new MethodCallStackMermaidService();
            var nodes = new List<MethodCallStackNode>
            {
                new(
                    "0_Test.Sample.Run.none_0",
                    0,
                    0,
                    "Test",
                    "Sample",
                    "Run",
                    Array.Empty<string>(),
                    "Test.Sample.Run.none",
                    "Test",
                    "Helper",
                    "DoWork",
                    Array.Empty<string>(),
                    "Test.Helper.DoWork.none",
                    null,
                    new MethodCallSite("./Sample.cs", 10, 5, 2, 1, 2, 5),
                    null,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    Array.Empty<string>(),
                    Array.Empty<string?>())
            };

            var diagram = service.BuildDiagram(nodes);

            Assert.Contains("sequenceDiagram", diagram);
            Assert.Contains("->>", diagram);
            Assert.Contains("-->>", diagram);
        }
    }
}