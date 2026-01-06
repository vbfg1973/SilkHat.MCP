using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services;

public sealed class SolutionParserTests
{
    [Fact]
    public void Parse_ReturnsSampleProjects()
    {
        var sampleRoot = LocateSampleRoot();
        var solutionPath = Path.Combine(sampleRoot, "SilkHat.Sample.sln");
        var parser = new SolutionParser();

        var solution = parser.Parse(solutionPath);

        Assert.Equal(2, solution.Projects.Count);
        Assert.Contains(solution.Projects, project => project.Name == "SilkHat.Sample.App");
        Assert.Contains(solution.Projects, project => project.Name == "SilkHat.Sample.Lib");
        Assert.All(solution.Projects, project =>
            Assert.EndsWith(".csproj", project.FullPath, StringComparison.OrdinalIgnoreCase));
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
