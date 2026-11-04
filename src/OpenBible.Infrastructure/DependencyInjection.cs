using Microsoft.Extensions.DependencyInjection;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.YesFile;

namespace OpenBible.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IYesFileReader, YesFileParser>();
        services.AddSingleton<IBibleRepository, InMemoryBibleRepository>();
        services.AddSingleton<IUserDataRepository, InMemoryUserDataRepository>();
        services.AddSingleton<ISearchIndex, SimpleSearchIndex>();

        return services;
    }
}
