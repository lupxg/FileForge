using FileForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileForge.Infrastructure.Persistence;

public class FileForgeDbContext : DbContext
{
    public FileForgeDbContext(DbContextOptions<FileForgeDbContext> options) : base(options) { }

    // DB sets.
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageSource> MessageSources => Set<MessageSource>();
    public DbSet<Conversation> Conversations => Set<Conversation>();


    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FileForgeDbContext).Assembly);
    }
}
