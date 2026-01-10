using Microsoft.Extensions.Logging.Abstractions;
using SilkHat.Code.Analysis.Models;
using SilkHat.Code.Analysis.Services;
using SilkHat.Code.Core.Dtos;

namespace SilkHat.Tests.Services;

public sealed class CodeTreeServiceSampleTests
{
    [Fact]
    public async Task GetTree_ReturnsTypeMembers_FromSampleSolution()
    {
        var sampleRoot = LocateSampleRoot();
        var loader = new CodeWorkspaceLoader(NullLogger<CodeWorkspaceLoader>.Instance);
        var workspace = await loader.LoadAsync(
            sampleRoot,
            new[] { new SolutionReference("./SilkHat.Sample.sln", "solution-1") },
            CancellationToken.None);
        var solution = Assert.Single(workspace.Solutions.Values);

        var entry = solution.TreeEntries.First(item =>
            item.Type == CodeTreeEntryType.File
            && item.DisplayPath.EndsWith("AnalysisSamples.cs", StringComparison.OrdinalIgnoreCase));

        var service = new CodeTreeService(new CodeSymbolOutlineService());
        var types = await service.GetTreeAsync(workspace, solution, entry.DisplayPath, CancellationToken.None);

        var complexityType = types.FirstOrDefault(type => type.Name == "ComplexitySamples");
        Assert.NotNull(complexityType);
        Assert.Equal(CodeTreeEntryType.Type, complexityType!.Type);
        Assert.False(string.IsNullOrWhiteSpace(complexityType.DisplayPath));

        var members = await service.GetTreeAsync(workspace, solution, complexityType.DisplayPath, CancellationToken.None);
        Assert.Contains(members, member => member.Name == "CalculateScore" && member.RealType == "Method");

        var helperType = types.FirstOrDefault(type => type.Name == "StatusHelper");
        Assert.NotNull(helperType);

        var helperMembers = await service.GetTreeAsync(workspace, solution, helperType!.DisplayPath, CancellationToken.None);
        Assert.Contains(helperMembers, member => member.Name == "Grade11PlusScore" && member.RealType == "Property");
        Assert.Contains(helperMembers, member => member.Name == "StatusChecked" && member.RealType == "Event");
    }

    private static string LocateSampleRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", "solution01", "SilkHat.Sample.sln");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate samples/solution01/SilkHat.Sample.sln.");
    }
}
