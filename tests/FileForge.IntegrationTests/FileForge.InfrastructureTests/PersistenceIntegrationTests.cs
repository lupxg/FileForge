using FileForge.Infrastructure.Persistence;
using FileForge.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FileForge.IntegrationTests.FileForge.InfrastructureTests;

public sealed class PersistenceIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Database_ShouldBeAccessible()
    {
        using var scope = fixture.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<FileForgeDbContext>();

        var canConnect = await db.Database.CanConnectAsync(TestContext.Current.CancellationToken);

        Assert.True(canConnect);
    }
}