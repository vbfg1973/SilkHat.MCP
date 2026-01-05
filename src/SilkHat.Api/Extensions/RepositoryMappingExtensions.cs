using SilkHat.Core.Dtos;
using SilkHat.Infrastructure.Entities;

namespace SilkHat.Api.Extensions;

public static class RepositoryMappingExtensions
{
    public static RepositoryConfigDto ToDto(this RepositoryConfig config)
    {
        return new RepositoryConfigDto(
            config.Id,
            config.Name,
            config.RootPath,
            config.Description,
            config.GroupId,
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
