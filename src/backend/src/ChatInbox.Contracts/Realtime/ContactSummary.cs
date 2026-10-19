namespace ChatInbox.Contracts.Realtime;

public sealed record ContactSummary(
    Guid Id,
    string FirstName,
    string? LastName,
    string? Username);
