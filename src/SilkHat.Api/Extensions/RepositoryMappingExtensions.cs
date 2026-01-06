using SilkHat.Core.Dtos;
using SilkHat.Infrastructure.Entities;
using System.Linq;

namespace SilkHat.Api.Extensions;

public static class RepositoryMappingExtensions
{
    public static RepositoryConfigDto ToDto(this RepositoryConfig config)
    {
        var solutions = config.Solutions
            .OrderBy(solution => solution.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(solution => new RepositorySolutionDto(solution.RelativePath, solution.IsEnabled, solution.SolutionId))
            .ToList();

        return new RepositoryConfigDto(
            config.Id,
            config.Name,
            config.RootPath,
            config.Description,
            config.GroupId,
            solutions,
            config.CreatedUtc,
            config.UpdatedUtc);
    }

    public static RepositoryGroupDto ToDto(this RepositoryGroup group)
    {
        return new RepositoryGroupDto(
            group.Id,
            group.Name,
            group.Description,
            group.CreatedUtc,
            group.UpdatedUtc);
    }
}
