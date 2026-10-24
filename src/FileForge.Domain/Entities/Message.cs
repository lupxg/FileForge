using System;
using System.Collections.Generic;
using System.Text;

namespace FileForge.Domain.Entities;

public class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    // Needed by EF Core.
    public Message() { }

    public static Message Create(
        Guid conversationId,
        string role,
        string content)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException(
                "Conversation ID is required.",
                nameof(conversationId));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException(
                "Message role is required.",
                nameof(role));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(
                "Message content is required.",
                nameof(content));

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
