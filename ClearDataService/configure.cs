using ClearDataService.Abstractions;
using ClearDataService.Contexts;
using ClearDataService.Exceptions;
using ClearDataService.Factory;
using ClearDataService.Migrators;
using ClearDataService.Models;
using ClearDataService.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Reflection;

namespace ClearDataService;

public static class ServiceExtensions
{
    public static IServiceCollection AddSqlDbContext(this IServiceCollection services)
    {
        services.AddScoped<ISqlDbContext, SqlDbContext>();
        return services;
    }

    public static IHost MigrateSqlDatabase(this IHost builder)
    {
        using IServiceScope scope = builder.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var db = scope.ServiceProvider.GetService<DbContext>();
        db?.Database.MigrateAsync().GetAwaiter().GetResult();

        return builder;
    }

    public static IServiceCollection AddCosmosDbContext(this IServiceCollection services,
        ICosmosDbSettings settings, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        var serializer = new CosmosJsonSerializer(jsonSerializerSettings);

        services.AddSingleton(settings);

        services.AddSingleton(CosmosDbClientFactory.CreateClient(settings, CreateCosmosClientOptions(serializer)));
        services.AddScoped<ICosmosDbContext, CosmosDbContext>();
        services.AddScoped<ICosmosDbMigrator, CosmosDbMigrator>();

        return services;
    }

    public static IServiceCollection AddCosmosDbContext(this IServiceCollection services,
        IConfiguration configuration, string appSettingsKey)
    {
        IConfigurationSection section = configuration.GetSection(appSettingsKey);

        var settings = CosmosDbSettings.Create
        (
            section.GetSection(nameof(CosmosDbSettings.EndpointUri)).Value ?? "",
            section.GetSection(nameof(CosmosDbSettings.PrimaryKey)).Value ?? "",
            section.GetSection(nameof(CosmosDbSettings.DatabaseName)).Value ?? ""
        );

        services.AddCosmosDbContext(settings);

        return services;
    }

    private static CosmosClientOptions CreateCosmosClientOptions(CosmosSerializer serializer)
    {
        return new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            PortReuseMode = PortReuseMode.PrivatePortPool,
            IdleTcpConnectionTimeout = new TimeSpan(0, 60, 0), //recommended values are from 20 minutes to 24 hours.
            Serializer = serializer
        };
    }

    //public static IApplicationBuilder CreateCosmosDatabaseAndContainers(this IApplicationBuilder builder,
    //    params IEnumerable<CosmosDbContainerInfo> containers)
    //{
    //    using IServiceScope scope = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
    //    CreateCosmosDatabaseAndContainers(scope, containers);

    //    return builder;
    //}

    public static IHost CreateCosmosDatabaseAndContainers(this IHost builder,
        params IEnumerable<CosmosDbContainerInfo> containers)
    {
        using IServiceScope scope = builder.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        CreateCosmosDatabaseAndContainers(scope, containers);

        return builder;
    }

    private static void CreateCosmosDatabaseAndContainers(IServiceScope scope,
        params IEnumerable<CosmosDbContainerInfo> containers)
    {
        var migrator = scope.ServiceProvider.GetService<ICosmosDbMigrator>();
        migrator?.CreateDatabaseAndContainers(containers).GetAwaiter().GetResult();
    }

    public static IApplicationBuilder CreateCosmosDatabaseAndContainersFromRepositories(this IApplicationBuilder builder)
    {
        using IServiceScope scope = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var migrator = scope.ServiceProvider.GetService<ICosmosDbMigrator>();
        migrator?.CreateDatabaseAndContainers(GetContainerNameFromRepositories()).GetAwaiter().GetResult();

        return builder;
    }

    private static List<CosmosDbContainerInfo> GetContainerNameFromRepositories()
    {
        var type = typeof(ICosmosDbRepo<>);

        var containers = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.ExportedTypes)
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type))
            .Select(t => new
            {
                Type = t,
                ContainerName = t.GetProperty("ContainerName", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string
            })
            .ToList();

        var missingContainers = containers.Where(x => string.IsNullOrEmpty(x.ContainerName)).ToList();

        if (missingContainers.Count > 0)
        {
            throw new ContainerNameMissingFromRepoException([.. missingContainers.Select(x => x.Type)]);
        }

        return [.. containers.Select(x => x.ContainerName!).Distinct()];
    }

    //public static IServiceCollection AddAllSqlDbRepo(this IServiceCollection services)
    //{
    //    var type = typeof(ISqlDbRepo<>);

    //    var repoTypes = GetRepoTypes(type);

    //    foreach (var repoType in repoTypes)
    //    {
    //        var interfaceType = repoType.GetInterfaces().FirstOrDefault(i => i != type);
    //        if (interfaceType != null)
    //        {
    //            services.AddScoped(interfaceType, repoType);
    //        }
    //    }

    //    return services;
    //}

    //public static IServiceCollection AddAllCosmosDbRepo(this IServiceCollection services)
    //{
    //    var type = typeof(ICosmosDbRepo<>);

    //    var repoTypes = GetRepoTypes(type);

    //    foreach (var repoType in repoTypes)
    //    {
    //        var interfaceType = repoType.GetInterfaces().FirstOrDefault(i => i != type);
    //        if (interfaceType != null)
    //        {
    //            services.AddScoped(interfaceType, repoType);
    //        }
    //    }

    //    return services;
    //}

    //private static List<Type> GetRepoTypes(Type type)
    //{
    //    var types = AppDomain.CurrentDomain
    //        .GetAssemblies()
    //        .SelectMany(a => a.ExportedTypes)
    //        .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type))
    //        .ToList();

    //    return types ?? [];
    //}
}