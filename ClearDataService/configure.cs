using Clear.DataService.Abstractions;
using Clear.DataService.Contexts;
using Clear.DataService.Factory;
using Clear.DataService.Migrators;
using Clear.DataService.Models;
using Clear.DataService.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Clear.DataService;

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
            AllowBulkExecution = true,
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

}
