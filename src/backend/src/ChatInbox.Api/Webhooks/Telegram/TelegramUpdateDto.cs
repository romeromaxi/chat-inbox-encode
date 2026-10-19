using System.Text.Json.Serialization;

namespace ChatInbox.Api.Webhooks.Telegram;

public sealed record TelegramUpdateDto(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramMessageDto? Message,
    [property: JsonPropertyName("edited_message")] TelegramMessageDto? EditedMessage);

public sealed record TelegramMessageDto(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("date")] long Date,
    [property: JsonPropertyName("chat")] TelegramChatDto? Chat,
    [property: JsonPropertyName("from")] TelegramUserDto? From,
    [property: JsonPropertyName("text")] string? Text);

public sealed record TelegramChatDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type);

public sealed record TelegramUserDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("username")] string? Username);
