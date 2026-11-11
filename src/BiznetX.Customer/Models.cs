using System.Text.Json;

namespace BiznetX.Customer;

public sealed record IdentifyCustomerRequest(
    string? Email = null,
    IReadOnlyDictionary<string, object?>? Attributes = null
);

public sealed record TrackEventRequest(
    Guid EventId,
    string CustomerId,
    string Name,
    DateTimeOffset? OccurredAt = null,
    IReadOnlyDictionary<string, object?>? Properties = null
);

public sealed record CustomerProfile(
    string ExternalId,
    string? Email,
    JsonElement Attributes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record EventReceipt(Guid EventId, bool Duplicate);
