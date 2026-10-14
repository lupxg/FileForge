using FileForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;
using System.Numerics;

namespace FileForge.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.DocumentId)
            .IsRequired();

        builder.Property(x => x.Index)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.TokenCount)
            .IsRequired();

        var converter =
            new ValueConverter<ReadOnlyMemory<float>, Pgvector.Vector>(
                embedding => new Pgvector.Vector(embedding.ToArray()),
                vector => new ReadOnlyMemory<float>(vector.ToArray()));

        builder.Property(x => x.Embedding)
            .HasConversion(converter)
            .HasColumnType("vector")
            .IsRequired();

        builder.HasIndex(x => new { x.DocumentId, x.Index })
            .IsUnique();

        builder.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(x => x.Index)
            .HasColumnName("chunk_index")
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(x => x.TokenCount)
            .HasColumnName("token_count")
            .IsRequired();

        builder.Property(x => x.Embedding)
            .HasColumnName("embedding")
            .HasConversion(converter)
            .HasColumnType("vector")
            .IsRequired();
    }
}
