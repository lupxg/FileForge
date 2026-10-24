using FileForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileForge.Infrastructure.Persistence.Configurations;

public sealed class MessageSourceConfiguration
    : IEntityTypeConfiguration<MessageSource>
{
    public void Configure(EntityTypeBuilder<MessageSource> builder)
    {
        builder.ToTable("message_sources");

        builder.HasKey(x => new { x.MessageId, x.ChunkId });

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id");

        builder.Property(x => x.ChunkId)
            .HasColumnName("chunk_id");

        builder.Property(x => x.Score)
            .HasColumnName("score")
            .IsRequired();

        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<DocumentChunk>()
            .WithMany()
            .HasForeignKey(x => x.ChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
