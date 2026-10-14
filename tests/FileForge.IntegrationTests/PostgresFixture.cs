
using FileForge.Infrastructure;
using FileForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FileForge.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public PostgresFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PostgresFixture>()
            .Build();

        var testConnectionString =
            configuration.GetConnectionString("FileForgeTest")
            ?? throw new InvalidOperationException(
                "Connection string 'FileForgeTest' was not configured.");

        // AddInfrastructure expects the key "FileForgeConnection".
        var infrastructureConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FileForgeConnection"] =
                        testConnectionString
                })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(infrastructureConfiguration);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        await db.Database.MigrateAsync();
    }

    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    public Task DisposeAsync()
    {
        return _serviceProvider.DisposeAsync().AsTask();
    }
}
