using FileForge.Infrastructure;
using FileForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileForge.IntegrationTests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public PostgresFixture()
    {
        // Read the test database connection string from User Secrets.
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PostgresFixture>()
            .Build();

        var testConnectionString =
            configuration.GetConnectionString("FileForgeTest")
            ?? throw new InvalidOperationException(
                "Connection string 'FileForgeTest' was not configured. " +
                "Set it using .NET User Secrets.");

        // AddInfrastructure expects the key FileForgeConnection.
        // Supply the test connection string under that key.
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

    public async ValueTask InitializeAsync()
    {
        // Apply migrations to the dedicated test database.
        await using var scope = _serviceProvider.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        await db.Database.MigrateAsync();
    }

    // Each test can create its own scope and DbContext.
    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }
}
