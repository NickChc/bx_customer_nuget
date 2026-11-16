# SDK conventions

Checkpoint workflow (user instruction, 2026-11-16): work through ready checkpoints and groups
sequentially, respecting dependencies. Read each checkpoint's
requirements and acceptance criteria; implement it, add/update meaningful tests,
run all required verification, fix failures, and review the diff for correctness,
regressions and security before committing. Commit completed checkpoints locally
without asking for confirmation, update the shared PLAN.md with status, commands,
results and remaining gates, then continue automatically across ready groups.
Do not push unless the user explicitly requests it. This supersedes the earlier
per-checkpoint commit/push approval rule. Stop only for required human credentials,
permissions or external decisions; an unplanned significant architectural decision;
a material deviation from approved requirements; or an unresolved blocker after
reasonable investigation. Never bypass failing tests or silently weaken verification.
The existing PLAN.md is the shared plan (the user's PLANS.md reference does not
require a second plan). Groups are planning boundaries, not mandatory prompt or session boundaries.
Record current scope and remaining groups; continue until intervention is required. Keep progress updates and human-only acceptance handoffs.

After every edit, format C# with repository-local CSharpier 0.30.6, matching the
user's VS Code save formatter: `dotnet tool restore`, then
`dotnet csharpier <edited paths>`. Do not substitute dotnet format. Before a
checkpoint run `dotnet csharpier src tests --check --no-cache`, then the full CI
locked restore, Release tests and pack. Supported JSON/Markdown/YAML uses
Prettier 3.7.4 with this repository's default `.prettierrc.json`, for example
`npx --yes prettier@3.7.4 --write <edited paths>`. Exclude secrets, generated
output and unsupported formats. Inspect changes and verify a second pass is
unchanged; do not change behavior or the SDK's naming convention while formatting.

Verify features with meaningful automated tests or reliable direct checks. For behavior the agent cannot reliably verify, tell the user "Before we move on you should try X, Y and Z" with concrete steps and expected results, and record pending manual verification in the shared PLAN.md. Do not claim unperformed checks passed or require redundant manual testing.

Package ID is `BiznetX.Customer`; target .NET 8+ consumers. Keep runtime dependencies small. Never reference backend projects or share their internal entity types. Mirror versioned HTTP contracts explicitly and test serialized payloads, errors, cancellation, and credentials.

In the full workspace, read the root AGENTS.md for shared product context. The SDK reports application customer state and events; campaign selection, delays, eligibility and messaging belong to the platform. Caller outbox/retry guidance concerns reliable ingestion only. Currently IdentifyAsync and TrackAsync are implemented; there is no SDK workflow engine or built-in casino event catalog.

Customer IDs and event IDs are project-scoped through credentials. Preserve identify-before-track and current attribute/null semantics. Accept arbitrary event names/properties within the API limits. Future optional predefined-event helpers must use the same TrackAsync contract and remain optional for non-casino applications. A successful event receipt acknowledges persistence/deduplication, not campaign execution or message delivery.

Expose asynchronous APIs with CancellationToken. Name const fields, local const declarations and constant-like static readonly fields in UPPER_SNAKE_CASE. Never generate event IDs inside retry loops, retry POST blindly, log keys, follow redirects with credentials, or suppress HTTP failures. Use HttpClientFactory integration and a typed exception preserving status. Keep JSON names camelCase to match the API. Follow semantic versioning; document breaking changes.

Run `dotnet test BiznetX.Customer.slnx` and `dotnet pack src/BiznetX.Customer -c Release`. Docker builds a package artifact. Do not publish a package unless requested.
