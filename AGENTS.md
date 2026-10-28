# SDK conventions

Package ID is `BiznetX.Customer`; target .NET 8+ consumers. Keep runtime dependencies small. Never reference backend projects or share their internal entity types. Mirror versioned HTTP contracts explicitly and test serialized payloads, errors, cancellation, and credentials.

Expose asynchronous APIs with CancellationToken. Never generate event IDs inside retry loops, retry POST blindly, log keys, follow redirects with credentials, or suppress HTTP failures. Use HttpClientFactory integration and a typed exception preserving status. Keep JSON names camelCase to match the API. Follow semantic versioning; document breaking changes.

Run `dotnet test BiznetX.Customer.slnx` and `dotnet pack src/BiznetX.Customer -c Release`. Docker builds a package artifact. Do not publish a package unless requested.
