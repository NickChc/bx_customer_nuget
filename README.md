# BiznetX.Customer

Typed .NET 8+ client for BX Customer's v1 customer/event ingestion API. This package does not reference backend projects.

```csharp
using BiznetX.Customer;

builder.Services.AddBxCustomer(options =>
{
    options.BaseAddress = new Uri(builder.Configuration["BxCustomer:BaseAddress"]!);
    options.ApiKey = builder.Configuration["BxCustomer:ApiKey"]!;
});

// Inject ICustomerClient. Identify before tracking events.
await customerClient.IdentifyAsync("player-42", new IdentifyCustomerRequest(
    "person@example.test",
    new Dictionary<string, object?> { ["language"] = "en" }), cancellationToken);

// Persist this ID in your application's outbox; reuse the exact request on retry.
var eventId = Guid.NewGuid();
await customerClient.TrackAsync(new TrackEventRequest(
    eventId, "player-42", "casino.deposit.completed",
    DateTimeOffset.UtcNow,
    new Dictionary<string, object?> { ["amountMinor"] = 2500, ["currency"] = "EUR" }), cancellationToken);
```

Custom event names are supported; the casino name above is just an example, not a built-in trigger. IDs and event names accept letters, digits, `_ . : @ -`, must begin with a letter/digit, and have a maximum length of 128.

Base URLs must end in `/` and use HTTPS except on loopback during local development. Configuration is validated on resolution/startup. Keys stay in the HTTP Authorization header. Redirect following is disabled. Keep credentials in your environment/secret manager.

Customer attributes merge at the top level; explicit null attribute values remove those keys. Omitted attributes/email remain unchanged. Email clearing is not yet supported. JSON objects are limited to 16 KiB and 100 top-level properties. Event IDs are unique per project. Repeating the same payload returns `Duplicate = true`; changing content returns HTTP 409. Identify updates are safe to retry as assignments, but concurrent updates to the same attribute use last-write-wins.

The SDK does not silently retry. Use a durable application outbox; reuse the event ID, timestamp and properties on timeouts, 429s and transient server errors. Inspect `CustomerApiException.StatusCode`, `Detail`, and `TraceId`. Do not retry validation/authentication/conflict responses unchanged.

## Build, test and package

```powershell
dotnet restore BiznetX.Customer.slnx --locked-mode
dotnet test BiznetX.Customer.slnx --no-restore
dotnet pack src/BiznetX.Customer -c Release --no-restore -o artifacts
docker build --output type=local,dest=artifacts/docker .
```

Docker produces a NuGet artifact, not a long-running service. Package publication to the GitLab registry is a separate release step.
