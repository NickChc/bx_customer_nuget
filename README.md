# BiznetX.Customer

Send customer profiles and application events to BX Customer from a .NET 8 or later backend. Use `IdentifyAsync` to create or update a customer and `TrackAsync` to report an event for that customer. BX Customer evaluates workflows after ingestion. An event receipt confirms persistence or deduplication.

## Install and configure

Add the package from a NuGet source that contains `BiznetX.Customer`. To try it directly from this repository, build a local package with the commands under **Build from source**, then run this in your application project:

```sh
dotnet nuget add source "/absolute/path/to/bx_customer_nuget/artifacts" --name bx-customer-local
dotnet add package BiznetX.Customer --version 0.1.1
```

Register the local source once. Keep nuget.org or your existing dependency mirror configured for the package's Microsoft dependencies.

In ASP.NET Core, set `BxCustomer__BaseAddress` to your installation's API base URL, including the trailing slash, and supply `BxCustomer__ApiKey` from your server's secret store. Use a project integration key for the destination workspace. Use HTTPS outside loopback and keep the key on the server.

```csharp
using BiznetX.Customer;

builder.Services.AddBxCustomer(options =>
{
    options.BaseAddress = new Uri(builder.Configuration["BxCustomer:BaseAddress"]!);
    options.ApiKey = builder.Configuration["BxCustomer:ApiKey"]!;
});
```

## Identify a customer and track an event

Inject `ICustomerClient` into your application service. Use a stable customer ID from your application. The following example sets a profile attribute and reports a custom event. Pass the event UUID and original occurrence time saved with the business operation.

```csharp
using BiznetX.Customer;

static async Task ReportPurchaseAsync(
    ICustomerClient client, string customerId, Guid eventId,
    DateTimeOffset occurredAt, CancellationToken cancellationToken)
{
    await client.IdentifyAsync(customerId, new IdentifyCustomerRequest(
        Attributes: new Dictionary<string, object?> { ["language"] = "en" }),
        cancellationToken);

    await client.TrackAsync(new TrackEventRequest(
        eventId, customerId, "purchase.completed", occurredAt,
        new Dictionary<string, object?> { ["amountMinor"] = 2500, ["currency"] = "EUR" }),
        cancellationToken);
}
```

Customer IDs and event names must start with an ASCII letter or digit and can contain up to 128 letters, digits, or `_ . : @ -`. Attributes and event properties each accept a JSON object up to 16 KiB with up to 100 top-level keys. Identify merges top-level attributes. An explicit null attribute value removes that key. Omitting attributes or email preserves existing values.

## Handle retries in your application

Store the event UUID, timestamp and complete payload in your application's durable outbox before the first send. The SDK makes one application-level attempt per call. Inspect `CustomerApiException.StatusCode`, `Detail` and `TraceId` for HTTP failures.

| Result                                       | What to do                                                                                         |
| -------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| Successful event receipt                     | Mark the event delivered, including when `Duplicate` is true.                                      |
| Timeout, lost response, 429 or transient 5xx | Retry with backoff using the original UUID and identical payload.                                  |
| 404 while tracking                           | Verify the workspace and identify the customer there before retrying.                              |
| 409                                          | The UUID already identifies different content. Investigate the outbox entry.                       |
| 410                                          | The customer has a permanent processing block. Stop sending profiles and events for that identity. |
| Validation or authentication error           | Correct the input or credentials before retrying.                                                  |

In your BX Customer website, open **Documentation → Application integration** (`/docs/application-integration`) for key setup, workspace selection, a synthetic workflow smoke test and the full consumer delivery guidance.

## Build from source

With the .NET 8 SDK or later, run these commands from the repository root:

```sh
dotnet restore tests/BiznetX.Customer.Tests --locked-mode
dotnet test tests/BiznetX.Customer.Tests -c Release --no-restore
dotnet pack src/BiznetX.Customer -c Release --no-restore -o artifacts
```

The package, including this README and its icon, is written to `artifacts/`. The Docker build also produces a package: `docker build --output type=local,dest=artifacts/docker .`.
