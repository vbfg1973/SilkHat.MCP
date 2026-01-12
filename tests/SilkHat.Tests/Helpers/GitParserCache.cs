using System.Collections.Concurrent;
using SilkHat.Git.Analysis.Services;
using SilkHat.Git.Core.Dtos;

namespace SilkHat.Tests.Helpers
{
    public static class GitParserCache
    {
        private static readonly ConcurrentDictionary<string, IReadOnlyList<GitCommitDto>> Cache =
            new(StringComparer.Ordinal);

        public static IReadOnlyList<GitCommitDto> Parse(string log)
        {
            return Cache.GetOrAdd(log, GitCommitParser.Parse);
        }
    }
}