# SDK conventions

Checkpoint workflow (user instruction, 2026-11-18): implement one manually
triggered batch of 3–5 related steps, scoped to take no more than 1–2 hours including
implementation, required tests, formatting, review and local commits. Split large
milestones at safe reviewable boundaries before starting; reserve verification time.
Continue within that batch without intermediate confirmation, then stop for review
and wait for the user's instruction before another batch. Stop earlier for required
credentials/permissions/external decisions, an unplanned significant architectural
decision, material scope deviation or a blocker unresolved after reasonable investigation.
If the budget becomes unrealistic, record a safe partial handoff rather than expanding
scope, weakening verification or claiming an unfinished checkpoint complete.
Read acceptance criteria, implement, add/update meaningful tests, run required checks,
fix failures and review correctness/regressions/security before each verified local
commit. Do not push unless explicitly requested. Preserve failed-run evidence.
Start at [PLAN.md](../PLAN.md), then read current status, the selected feature,
its applicable global constraints and actual dependency contracts only. Do not
recursively read all plan/history files or PLAN.original.md. Update feature tasks,
current status and the relevant evidence; recover context from these plus Git state.
See [working method](../docs/plan/working-method.md). This supersedes all older
automatic-continuation instructions across groups. Original numbered groups are
milestones, not prompt durations. Do not edit TODO_human_dev.md unless asked.

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
