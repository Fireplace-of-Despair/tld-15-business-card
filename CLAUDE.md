# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

TLD-15 — a personal portfolio / business-card website (projects, articles, contacts, RSS). Blazor
Server (`InteractiveServer` render mode) on **.NET 11 preview**, PostgreSQL, Serilog, AGPL-3.0-only.
The `Stainless*` projects are a reusable application skeleton shared with a sibling projects; `tld15Server` is the site itself.

## Commands

```bash
dotnet build tld15.slnx
```

```bash
dotnet run --project tld15Server
```

The dev profile serves `http://localhost:5105` (`tld15Server/Properties/launchSettings.json`).

```bash
dotnet test tld15.slnx
```

`global.json` selects the **Microsoft.Testing.Platform** runner (xUnit v3), so filters are MTP
options passed after `--`, not `--filter`:

```bash
dotnet test Tests/tld15ServerTests/tld15ServerTests.csproj -- --filter-method "*IdentityPostFeature_Tests*"
```

Also available: `--filter-class`, `--filter-namespace`, `--filter-trait "Application=Integration Tests"`,
`--filter-not-*`, `--list-tests`. The test project is an executable, so
`dotnet run --project Tests/tld15ServerTests -- --help` lists every option.

```bash
docker build -f tld15Server/Dockerfile -t tld15-server:latest .
```

Build the image **from the repository root** — every `COPY` in the Dockerfile is root-relative.

### Gotchas

- A running dev server locks `bin/Debug/net11.0/*.dll`, and the build then fails with MSB3027.
  Stop the app first, or build `-c Release` into a separate output.
- Integration tests need a live PostgreSQL. `appsettings.Tests.json` points at `localhost:5432`,
  database `tld15-local-test`, user `postgres`. The assembly fixture
  (`Tests/tld15ServerTests/AssemblyInfo.cs`) runs the migrations before any test.

## Architecture

Four projects plus tests (`tld15.slnx`):

| Project | Role |
|---|---|
| `StainlessCore` | Shared kernel: `Execute`, incidents, feature/endpoint contracts, hashing, API keys, login throttle. Documented library — `CS1591` is *not* suppressed here. |
| `StainlessInfrastructure` | EF Core `DbContext` per schema, FluentMigrator migrations, reference-data import. |
| `StainlessGenerators` | Roslyn incremental generator (`netstandard2.0`), referenced as an analyzer. |
| `tld15Server` | Blazor Server UI, minimal-API endpoints, features, composition. |
| `Tests/tld15ServerTests` | xUnit v3. `tld15Server` grants it `InternalsVisibleTo`. |

### Vertical slices: features

Every unit of work is one file under `tld15Server/Features/<Area>/<Name>Feature.cs` implementing
`IFeature` and containing everything it needs:

```csharp
public class HomeGetFeature : IFeature
{
    public const string Id = "home.get";        // dotted, lowercase
    public static string FeatureId => Id;

    public sealed class Result { ... }
    public sealed record Query : IQuery<Result> { ... }   // Mediator; ICommand for writes
    public sealed class Handler(IDbContextFactory<DataContextBusiness> factory)
        : IQueryHandler<Query, Result> { ... }
}
```

`Id` is also the **authorization policy name and claim value**. `StainlessGenerators` finds every
`IFeature` at compile time and emits `StainlessGenerated.Registry.FeatureIds`; startup turns each
into a policy requiring a `Feature` claim. Adding a feature class is the whole registration step —
never add a reflection scan or a manual list.

### Endpoints

`tld15Server/Endpoints/{Public,Protected,External}/…` implement `IEndpointPublic`,
`IEndpointProtected`, or `IEndpointExternal` — static-abstract `Metadata` (carrying the feature id)
and `ConfigureRouting`. The same generator emits `Registry.MapPublic/MapProtected/MapExternal`, wired
in `ServiceInjection.AddEndpoints` onto `/api/public`, `/api/protected`, `/api/external`.
Protected routes sit behind `ApiKeyFilter` (`X-API-KEY`, hashed) plus a rate-limit policy; the
`ExceptionHandlingFilter` registers *first* so it wraps the key check.

### Error handling

Features throw `IncidentException(IncidentCode)`. Blazor pages wrap the call in `Execute.Run(...)`
and read `Result.Data` / `Result.IncidentCode` instead of catching; API endpoints get the same
translation from `ExceptionHandlingFilter`. `IncidentCode` values are deliberately obfuscated
(1_401, 2_000, …) and mapped to HTTP codes by `IncidentCodeExtension.ToHTTPCode`.

### Middleware order (`tld15Server/Program.cs`)

The order is load-bearing and each step carries a comment explaining why. `UseForwardedHeaders` must
run first (rate-limit partitions, session metadata checks and cookie policy all read the caller
address), `UseRateLimiter` next, and `UseAuthentication`/`UseAuthorization` are called explicitly so
they land *behind* forwarded headers rather than at the framework default position. Do not reorder.

### Data access

One `DbContext` per PostgreSQL schema — `identity`, `reference`, `business`, `archive`, `settings`
(`StainlessInfrastructure/Composition/Globals.Schema`). All are registered as `AddDbContextFactory`,
and handlers create a context per unit of work rather than injecting one.

**EF Core does not own the schema.** Tables, triggers, constraints and seed data come from
FluentMigrator classes in `StainlessInfrastructure/Migrations`, named
`V<yyyy>_<MM>_<dd>_<HHmm>_<Name>.cs` with a matching `[Migration(2026_08_31_1336, "Init: Business")]`.
`MigrationRunner.Up` runs at application start and in the test fixture; the version table lives in
`system.version`. Helpers in `MigrationHelper` attach the `version_local` / `version_global` bump
triggers and load reference rows from `StainlessInfrastructure/.import/*.json` (copied to output).

### Sessions, auth and cache

Cookie authentication (`tld15-main`), but session state lives in `CacheManager` (in-memory), keyed by
a `Session` claim. `CookieEvent.ValidatePrincipal` re-checks every request against that cache — a
missing session, a changed feature set, or changed request metadata signs the user out. API-key
hashes are loaded into the same cache at startup (`Program.AddAllApiKeysToTheCache`). A restart
therefore drops all sessions by design. `CacheManager` carries a `TODO` about needing a refactor.

### Frontend

- Pages inherit `BasePage` (`Mediator`, localizer, cancellation token `_cts`, `IncidentCode`,
  `IsLoading`, `HasFeatureAsync`, browser-local time) and live in `Frontend/Pages/<Area>/`, split as
  `X.razor` + `X.razor.cs` + `X.razor.css`.
- Each page declares `public const string Url` in its code-behind and routes with
  `@attribute [Route(X.Url)]`. Navigation, redirects and 404 re-execution all reference those
  constants — never hard-code a path string.
- Localization is `en` + `ja` via `Frontend/Localization/Resources.resx` and `Resources.ja.resx`
  (keep both in sync) plus `LocalizationMiddleware`, which sets the culture from the
  `tld15-language` cookie. `InvariantGlobalization` is off and satellite languages are pinned to
  `en;ja` — the Docker runtime uses the `-extra` tag so ICU and tzdata are present.
- Styling is a global `wwwroot/app.css` with CSS custom properties (dark palette, `Park Lane NF`
  display font) plus per-component scoped CSS. `Frontend/Components/Icons/*.razor` are inline SVGs.
- Magic strings (content ids, cookie names, claim types, config keys, page metadata) belong in
  `tld15Server/Composition/Globals`.

## Conventions

- Every `.cs` file starts with `// SPDX-License-Identifier: AGPL-3.0-only` and the copyright line.
- `ImplicitUsings` is off — write explicit `using` directives, `System` first, outside the namespace.
  File-scoped namespaces. CRLF, 4 spaces (`.sh` files stay LF via `.gitattributes`).
- `EnforceCodeStyleInBuild` is on for every project and `.editorconfig` is the source of truth.
- Avoid reflection-based configuration binding and startup scans: the skeleton is written to stay
  trim-friendly (see the remarks on `StainlessCore.Composition.ServiceInjection.ReadEntries`).

## Deployment

`.github/workflows/rollout.yml` fires on push to `main`: it builds the image, rsyncs the tarball to
the server over SSH, then reloads Docker Compose. `.deploy/` holds the four server-side files
(`docker-compose.yml`, `Caddyfile.example`, `address-lists.sh`, `appsettings.example.json`) and
`.deploy/README.md` walks a bare VM to a running HTTPS site. Secrets (`Security:Pepper`, connection
string) live only in the server's `appsettings.json` — `.dockerignore` keeps every `appsettings.json`
out of the image. Adding a project to the solution means adding it to **both** Dockerfile stages
(restore copies the `.csproj`, publish copies the sources).
