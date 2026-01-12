using SilkHat.Code.Analysis.Services;

namespace SilkHat.Tests.Services
{
    public sealed class ProjectParserTests
    {
        [Fact]
        public void Parse_CollectsReferencesPackagesAndCompileItems()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), $"silkhat-project-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempRoot);

            var libDir = Path.Combine(tempRoot, "Lib");
            var appDir = Path.Combine(tempRoot, "App");
            Directory.CreateDirectory(libDir);
            Directory.CreateDirectory(appDir);

            var libProjectPath = Path.Combine(libDir, "Lib.csproj");
            var appProjectPath = Path.Combine(appDir, "App.csproj");

            File.WriteAllText(libProjectPath, """
                                              <Project Sdk="Microsoft.NET.Sdk">
                                                <PropertyGroup>
                                                  <TargetFramework>net9.0</TargetFramework>
                                                </PropertyGroup>
                                              </Project>
                                              """);

            var nestedDir = Path.Combine(appDir, "Nested");
            Directory.CreateDirectory(nestedDir);

            File.WriteAllText(Path.Combine(appDir, "Explicit.cs"), "namespace Demo; public class Explicit { }");
            File.WriteAllText(Path.Combine(nestedDir, "Other.cs"), "namespace Demo; public class Other { }");
            File.WriteAllText(Path.Combine(appDir, "Ignored.cs"), "namespace Demo; public class Ignored { }");

            File.WriteAllText(appProjectPath, """
                                              <Project Sdk="Microsoft.NET.Sdk">
                                                <PropertyGroup>
                                                  <TargetFramework>net9.0</TargetFramework>
                                                  <AssemblyName>App.Assembly</AssemblyName>
                                                  <RootNamespace>App.Root</RootNamespace>
                                                  <DefineConstants>DEBUG;TRACE;CUSTOM</DefineConstants>
                                                  <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                                                </PropertyGroup>
                                                <ItemGroup>
                                                  <ProjectReference Include="..\\Lib\\Lib.csproj" />
                                                </ItemGroup>
                                                <ItemGroup>
                                                  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                                                  <PackageReference Include="Dapper">
                                                    <Version>2.1.0</Version>
                                                  </PackageReference>
                                                </ItemGroup>
                                                <ItemGroup>
                                                  <Compile Include="Explicit.cs" />
                                                  <Compile Include="Nested\\Other.cs" />
                                                </ItemGroup>
                                              </Project>
                                              """);

            try
            {
                var parser = new ProjectParser();
                var parsed = parser.Parse(appProjectPath);

                Assert.Equal("App.Assembly", parsed.AssemblyName);
                Assert.Contains("CUSTOM", parsed.DefineConstants);
                Assert.Contains("net9.0", parsed.TargetFrameworks);
                Assert.False(parsed.EnableDefaultCompileItems);

                Assert.Contains(parsed.ProjectReferences, reference =>
                    reference.FullPath.Equals(libProjectPath, StringComparison.OrdinalIgnoreCase));

                Assert.Contains(parsed.PackageReferences, package =>
                    package.Id == "Newtonsoft.Json" && package.Version == "13.0.3");
                Assert.Contains(parsed.PackageReferences, package =>
                    package.Id == "Dapper" && package.Version == "2.1.0");

                Assert.Equal(2, parsed.CompileItems.Count);
                Assert.Contains(parsed.CompileItems,
                    path => path.EndsWith("Explicit.cs", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(parsed.CompileItems,
                    path => path.EndsWith("Other.cs", StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(parsed.CompileItems,
                    path => path.EndsWith("Ignored.cs", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}