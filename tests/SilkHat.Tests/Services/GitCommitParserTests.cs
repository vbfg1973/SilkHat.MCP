using SilkHat.Git.Core.Dtos;
using SilkHat.Tests.Helpers;

namespace SilkHat.Tests.Services
{
    public sealed class GitCommitParserTests
    {
        [Fact]
        public void Parse_ExtractsCommitMetadataAndChanges()
        {
            var output = string.Join('\n', new[]
            {
                "COMMIT|034d90007de0b599ff6d37724607e3c897fd435d|034d900|abcdef0|Chris Russell|cgrussell@gmail.com|2026-01-07T08:49:55+00:00|M6: add scripts and docs for hardened workflows",
                "BODY_BEGIN",
                "- add helper scripts for build/test/run and docker",
                "- expand docs (architecture, operations, API notes, troubleshooting)",
                "BODY_END",
                "M\tdocs/README.md",
                "A\tdocs/api.md",
                "R100\tdocs/old.md\tdocs/new.md",
                "COMMIT|284498fa724d30042742a3a4d7f2123d850a9a69|284498f|ed98d91 034d90007de0b599ff6d37724607e3c897fd435d|Chris Russell|cgrussell@gmail.com|2026-01-07T08:51:06+00:00|Merge pull request #14 from vbfg1973/feature/milestone06",
                "BODY_BEGIN",
                "",
                "BODY_END"
            });

            var commits = GitParserCache.Parse(output);

            Assert.Equal(2, commits.Count);

            var first = commits[0];
            Assert.Equal("034d90007de0b599ff6d37724607e3c897fd435d", first.CommitSha);
            Assert.Equal("034d900", first.AbbreviatedSha);
            Assert.Equal("Chris Russell", first.AuthorName);
            Assert.Equal("cgrussell@gmail.com", first.AuthorEmail);
            Assert.Equal("M6: add scripts and docs for hardened workflows", first.Subject);
            Assert.Contains("- add helper scripts", first.Body);
            Assert.False(first.IsMerge);
            Assert.Equal(3, first.Changes.Count);
            Assert.Contains(first.Changes,
                change => change.ChangeKind == GitChangeKind.Modify && change.Path == "./docs/README.md");
            Assert.Contains(first.Changes,
                change => change.ChangeKind == GitChangeKind.Add && change.Path == "./docs/api.md");
            Assert.Contains(first.Changes,
                change => change.ChangeKind == GitChangeKind.Rename && change.OldPath == "./docs/old.md" &&
                          change.Path == "./docs/new.md");

            var merge = commits[1];
            Assert.True(merge.IsMerge);
            Assert.Equal(2, merge.ParentShas.Count);
        }
    }
}