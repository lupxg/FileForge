using FileForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(x => x.Id);

        var converter = new ValueConverter<List<Guid>, string>(
            ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<Guid>>(
                json,
                (JsonSerializerOptions?)null) ?? new List<Guid>());

        var comparer = new ValueComparer<List<Guid>>(
            (left, right) => left != null && right != null
                ? left.SequenceEqual(right)
                : left == right,
            ids => ids.Aggregate(
                0,
                (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
            ids => ids.ToList());

        builder.Property<List<Guid>>("_documentIds")
            .HasField("_documentIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(converter)
            .Metadata.SetValueComparer(comparer);

        builder.Property<List<Guid>>("_documentIds")
            .HasColumnName("document_ids")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}