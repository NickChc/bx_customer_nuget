# SDK conventions

Git checkpoints: at each logical, verified milestone, tell the user 'We should push this changes with commit name "<proposed commit title>"'. Wait for the user's agreement before committing and pushing that checkpoint. For changes spanning multiple repositories, identify the affected repositories and propose an appropriate title for each. Keep checkpoints small and coherent rather than accumulating unrelated local changes. Agreement applies to the proposed checkpoint, not future pushes.

Verify features with meaningful automated tests or reliable direct checks. For behavior the agent cannot reliably verify, tell the user "Before we move on you should try X, Y and Z" with concrete steps and expected results, and record pending manual verification in the shared PLAN.md. Do not claim unperformed checks passed or require redundant manual testing.

Package ID is `BiznetX.Customer`; target .NET 8+ consumers. Keep runtime dependencies small. Never reference backend projects or share their internal entity types. Mirror versioned HTTP contracts explicitly and test serialized payloads, errors, cancellation, and credentials.

In the full workspace, read the root AGENTS.md for shared product context. The SDK reports application customer state and events; campaign selection, delays, eligibility and messaging belong to the platform. Caller outbox/retry guidance concerns reliable ingestion only. Currently IdentifyAsync and TrackAsync are implemented; there is no SDK workflow engine or built-in casino event catalog.

Customer IDs and event IDs are project-scoped through credentials. Preserve identify-before-track and current attribute/null semantics. Accept arbitrary event names/properties within the API limits. Future optional predefined-event helpers must use the same TrackAsync contract and remain optional for non-casino applications. A successful event receipt acknowledges persistence/deduplication, not campaign execution or message delivery.

Expose asynchronous APIs with CancellationToken. Name const fields, local const declarations and constant-like static readonly fields in UPPER_SNAKE_CASE. Never generate event IDs inside retry loops, retry POST blindly, log keys, follow redirects with credentials, or suppress HTTP failures. Use HttpClientFactory integration and a typed exception preserving status. Keep JSON names camelCase to match the API. Follow semantic versioning; document breaking changes.

Run `dotnet test BiznetX.Customer.slnx` and `dotnet pack src/BiznetX.Customer -c Release`. Docker builds a package artifact. Do not publish a package unless requested.
