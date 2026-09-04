<h1 align="center">TLD-15</h1>
<p align="center" width="100%">
    <img alt="Logo" width="400" src="https://github.com/Fireplace-of-Despair/tld-15-business-card/blob/main/tld15Server/wwwroot/images/logo_card.png?raw=true"/>
</p>

<p align="center">
  <b>The website of Fireplace of Despair.</b><br/>
  Our lore, our projects, our articles, and the ways to reach us.
</p>

<p align="center">
  <img alt="License" src="https://img.shields.io/badge/license-AGPL--3.0--only-blue"/>
  <img alt="Platform" src="https://img.shields.io/badge/.NET-11-512BD4"/>
  <img alt="UI" src="https://img.shields.io/badge/UI-Blazor%20Server-512BD4"/>
  <img alt="Database" src="https://img.shields.io/badge/database-PostgreSQL-336791"/>
  <img alt="Languages" src="https://img.shields.io/badge/languages-en%20%7C%20ja-lightgrey"/>
</p>

---

## What this is

TLD-15 is a business card. One organization runs one copy of it, and that copy is ours. It carries the
lore, the works, the articles, the press that mentions us, and the addresses that reach us. It tracks nobody, and asks a reader for no account.

It is not a product, and it is not a general-purpose CMS. Nothing here is built for a second tenant.
The code is open all the same, and the AGPL says what you may do with it: run it, change it, publish
your changes. Fork it and the site becomes yours to fill.

| | |
|---|---|
| **Static where it counts** | The public pages declare no render mode. A reader receives the whole page in the first response and opens no circuit. |
| **Content lives in the database** | Markdown in two locales, edited on the admin pages. No rebuild ships a new article. |
| **Bilingual** | English and Japanese, from the interface down to the reference data. |
| **Feeds without a library** | RSS 2.0, a sitemap and a `robots.txt`, each written as plain XML or text. |
| **Small to host** | One container and one PostgreSQL database, behind Caddy. |
| **Markdown only** | Stored prose is markdown, never HTML. One component renders it, and the renderer lets no HTML through. |
| **Posters are addresses** | No editor writes to disk. A picture is a URL that a policy approves. |

## What the site holds

**The front page** (`/`)

- The lore block, with a poster to the left of the prose.
- The works of the organization, as cards.
- The articles, as cards.
- The social addresses and the contacts, as icon buttons.
- A row of local anchors, one per block the page actually renders.

**The works**

- A work is a project or an article. Both live in one table and split by their type.
- Every work carries a division, a publication date, a poster, per-locale titles and a markdown body.
- A work of the Ashen Chronicles Division stays off the front page and appears on `/archive` instead.
- `/projects/{id}` is the public page for a work. It sets the canonical link, the OpenGraph and
  `article:*` meta, a `schema.org` block, and answers a real **404** for an id the table does not carry.

**The press**

- A mention is an outward address, a poster and a date. It carries no body.
- `/press` draws the mentions with the same card the works use.

**The feeds**

- `/rss` reads per request and carries the 20 newest works. A reader polls a feed once, so an
  announcement must not wait for the next deploy.
- `/sitemap.xml` is built once at start out of `Application:Host`, the front page, and one address per
  work that carries a translation.
- `/robots.txt` is composed, not a file. Without `Application:Host` it leaves the `Sitemap:` directive
  out rather than writing it relative.

**The admin**

- Every page that manages the site sits under `/admin`, which is the one line `robots.txt` has to carry.
- Works, press mentions, contents, accounts, sessions and the profile all have their own pages.
- A visitor loads no interactive component at all. The circuit begins after a sign-in.

## Routes

| Route | Purpose |
|---|---|
| `/` | The front page. |
| `/projects/{id}` | One work, project or article. |
| `/archive` | The works of the Ashen Chronicles Division. |
| `/press` | The mentions. |
| `/rss` | RSS 2.0, the 20 newest works. |
| `/sitemap.xml` | The sitemap, built at start. |
| `/robots.txt` | The robots file, composed at request. |
| `/identity/login` | Sign in. The first sign-in creates the first account. |
| `/settings/language` | Switch between `en` and `ja`. |
| `/admin/…` | Every page that manages the site. |
| `/404` | The page a missing address re-executes into. |
| `GET /api/public/ping` | Answers `pong`. The container health check reads it. |
| `GET /api/public/source` | The address of the source code that this build runs. |
| `GET /api/protected/ping` | The same answer, behind an `X-API-KEY` header. |

## How a page reaches a reader

```
   Reader                                Server
   ------                                ------
1. GET /                        -->  UseForwardedHeaders, then the rate limiter
2.                                   LocalizationMiddleware reads the tld15-language cookie
3.                                   HomeGetFeature asks for every block the page draws
4.                                   MarkdownService renders the stored markdown and caches it
5.                              <--  one static HTML response; no _blazor/negotiate follows

6. GET /projects/<id>           -->  ProjectGetFeature; canonical, OpenGraph, ld+json
                                     an id the table does not carry answers 404, not a friendly 200

7. GET /rss                     -->  RssGetFeature, read per request, written with XDocument
8. GET /sitemap.xml             -->  handed out of SitemapService, built once at start
```

The middleware order is load-bearing. `UseForwardedHeaders` runs first, because the rate-limit
partitions, the session metadata checks and the cookie policy all read the caller address.
`UseAuthentication` and `UseAuthorization` are called explicitly, so they land behind it.

## Repository layout

| Path | Contents |
|---|---|
| `StainlessCore` | Shared abstractions: `Execute`, incidents, feature and endpoint contracts, hashing, API keys, the login throttle. |
| `StainlessInfrastructure` | EF Core contexts, models, FluentMigrator migrations, reference-data import. |
| `StainlessGenerators` | The Roslyn generator that writes the feature list and the endpoint mappers. |
| `tld15Server` | The web application: Blazor Server UI, Minimal-API endpoints, features, services. |
| `Tests/tld15ServerTests` | xUnit v3 tests for the server. |
| `.deploy` | The four files a server needs, and the deployment guide. |
| `.github/workflows` | The roll-out that fires on a push to `main`. |

## Prerequisites

| Need | Item |
|---|---|
| Build | [.NET 11 SDK](https://dotnet.microsoft.com/download) |
| Database | [PostgreSQL](https://hub.docker.com/_/postgres) |
| Container build | [Docker](https://www.docker.com/) or [Podman](https://podman.io/) |
| Editor | [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) |
| Optional | [DBeaver](https://dbeaver.io/), [Sourcetree](https://www.sourcetreeapp.com/), [Lazygit](https://github.com/jesseduffield/lazygit) |

## Quick start

1. Create the database.

```sql
CREATE DATABASE "tld15-local";
```

2. Copy `tld15Server/appsettings.Development.json` to `tld15Server/appsettings.json`. The second file
   is git-ignored, and the server reads it first.

3. Set your connection string in `tld15Server/appsettings.json`.

```json
"ConnectionStrings": {
  "PostgreSQL": "Server=127.0.0.1;Port=5432;Database=tld15-local;UserId=postgres;Password=sa;"
}
```

4. Run the application. Migrations apply at startup.

```bash
dotnet run --project tld15Server
```

The application listens on `http://localhost:5105`.

5. Open `/identity/login` and sign in. The first sign-in creates the account and grants it every
   feature. There is no registration page, and no second account appears by itself.

6. Fill the site from `/admin`: the contents first, then the works and the press.

Sessions live in an in-memory cache, so a restart signs everybody out. This is by design.

### Gotchas

- A running dev server locks `bin/Debug/net11.0/*.dll`, and a build then fails with MSB3027. Stop the
  application first, or build `-c Release` into a separate output.
- Integration tests need a live PostgreSQL. `Tests/tld15ServerTests/appsettings.Tests.json` points at
  `localhost:5432` and the database `tld15-local-test`. The assembly fixture runs the migrations
  before any test.

## Deploy

The server publishes as one container image. Build it **from the repository root**, because every
`COPY` line in the Dockerfile reads from there.

```bash
docker build -f tld15Server/Dockerfile -t tld15-server:latest .
```

[.deploy/README.md](.deploy/README.md) holds the whole deployment: the Compose file, the Caddy reverse
proxy, the address lists, and the steps that take a bare virtual server to a running site behind HTTPS.

`.github/workflows/rollout.yml` fires on a push to `main`. It builds the image, ships the gzipped
tarball over SSH, reloads Compose, and waits for the container health check to report healthy before it
prunes the image it replaced. A failed verification leaves the replaced image in place.

## Configuration

The server reads `tld15Server/appsettings.json`. That file is git-ignored. Copy
`tld15Server/appsettings.Development.json` to start from a working set of keys, and
`.deploy/appsettings.example.json` for a server.

| Key | Purpose |
|---|---|
| `ConnectionStrings:PostgreSQL` | The database address. |
| `Application:Host` | The absolute address of the site. The canonical link, the sitemap and `robots.txt` read it. |
| `Application:SourceUrl` | The address of the source code that this build runs. |
| `Application:TwitterAccount` | The account that the share cards name. |
| `Security:Pepper` | The secret that the server mixes into password and API-key hashes. Change it. |
| `Security:CookieExpiration`, `Security:CookieMaxAge` | The life of the authentication cookie, in minutes. |
| `Security:LoginMaxAttempts` | Sign-in failures per account before a lockout. |
| `Security:LoginMaxAttemptsPerAddress` | Sign-in failures per client address before a lockout. |
| `Security:LoginLockoutMinutes` | The lockout duration. |
| `Security:ForwardedHeaders:KnownProxies` | Proxy addresses that may set `X-Forwarded-For`. |
| `Security:ForwardedHeaders:KnownNetworks` | Proxy networks in CIDR form. |
| `Security:RateLimit` | The window and the permits, globally and for the API. |
| `Serilog` | Log levels and sinks. |

> **When you run a modified copy for other people:** point `Application:SourceUrl` at your own fork.
> The AGPL asks you to offer your source to every remote user, and `GET /api/public/source` reads this
> key.

> **Without `Application:Host`:** `/sitemap.xml` answers 404, and `robots.txt` carries no `Sitemap:`
> directive. Neither one writes a relative address instead.

> **Behind a reverse proxy:** list your proxy in `KnownProxies` or `KnownNetworks`. An empty list keeps
> the framework default, which trusts loopback only. Every client then looks like the proxy, and one
> attacker locks out all users at once.

## Testing

Tests run on Microsoft.Testing.Platform. The root `global.json` selects the runner, so filters are
platform options after `--`, and not `--filter`.

```bash
dotnet test tld15.slnx
```

```bash
dotnet test Tests/tld15ServerTests/tld15ServerTests.csproj -- --filter-method "*IdentityPostFeature_Tests*"
```

`--filter-class`, `--filter-namespace`, `--filter-trait` and the `--filter-not-*` family work the same
way. The test project is an executable, so `dotnet run --project Tests/tld15ServerTests -- --help` lists
every option.

`EnforceCodeStyleInBuild` is on for every project, and `.editorconfig` is the source of truth. A build
reports a style violation on its own.

```bash
dotnet format tld15Server/tld15Server.csproj
```

## Architecture

### Vertical slices

A feature is one file under `Features/<Area>/<Name>Feature.cs`. The file holds the `Command` or `Query`,
the `Result`, and the `Handler` in one class.

```csharp
public sealed class HomeGetFeature : IFeature
{
    public const string Id = "home.get";        // dotted, lowercase
    public static string FeatureId => Id;       // required by IFeature

    public sealed class Result { /* ... */ }
    public sealed record Query : IQuery<Result> { /* ... */ }
    public sealed class Handler(/* dependencies */) : IQueryHandler<Query, Result> { /* ... */ }
}
```

[Mediator](https://github.com/martinothamar/Mediator) dispatches commands and queries in process, and
its own generator writes the registration.

The static `Id` carries weight. `StainlessGenerators` finds every `IFeature` at compile time and writes
`StainlessGenerated.Registry.FeatureIds`. At startup each id becomes an authorization policy that asks
for a `Feature` claim of the same value. Adding a feature class is the whole registration step: there is
no reflection scan, and no hand-kept list.

Handlers resolve `IDbContextFactory<T>` and create one `DbContext` per unit of work.

### Endpoints

Endpoints live under `Endpoints/{Public,Protected,External}/` and implement the matching interface from
`StainlessCore`. The same generator writes one mapper per interface, and `AddEndpoints` calls
`Registry.MapPublic`, `Registry.MapProtected` and `Registry.MapExternal` into their route groups.

| Group | Route | Purpose | Filters |
|---|---|---|---|
| `IEndpointPublic` | `/api/public` | Open to anyone. | `ExceptionHandlingFilter` |
| `IEndpointProtected` | `/api/protected` | An API key in `X-API-KEY`. | `ExceptionHandlingFilter`, then `ApiKeyFilter`, plus a rate-limit policy |
| `IEndpointExternal` | `/api/external` | Reserved for web-hooks and third-party integration. | `ExceptionHandlingFilter` |

The order in the protected group matters. `ExceptionHandlingFilter` registers first, so it wraps
`ApiKeyFilter`. An `IncidentException` from the key check then reaches the caller as an `Incident`, and
not as a raw error.

### Errors: incidents

Code throws `Exception` or `IncidentException(IncidentCode.X)`. A Blazor page wraps the call in
`Execute.Run(...)` and reads `Result.Data` or `Result.IncidentCode` instead of catching.
`ExceptionHandlingFilter` performs the same translation for the API: it logs the real error with Serilog
and returns an obfuscated `Incident`.

A generic exception becomes `General = 1_000`. A specific incident maps to a client-actionable HTTP code
through `IncidentCodeExtension.ToHTTPCode()`, for example `Unauthorized = 1_401` and
`WrongPassword = 2_000`.

### Data layer

`StainlessInfrastructure` holds one `DbContext` per domain, and each context maps to its own PostgreSQL
schema.

| Context | Schema | Domain |
|---|---|---|
| `DataContextIdentity` | `identity` | Accounts, features, API keys. |
| `DataContextBusiness` | `business` | Works, press, contents, and their translations. |
| `DataContextReference` | `reference` | Divisions, project types, content types, account types. |
| `DataContextArchive` | `archive` | Archived data. |
| `DataContextSettings` | `settings` | Settings. |

**EF Core does not own the schema.** Tables, triggers, constraints and seed rows come from FluentMigrator
classes under `StainlessInfrastructure/Migrations`, named
`V%yyyy%_%MM%_%dd%_%HHmm%_Short_Description.cs`. `MigrationRunner.Up` runs at application start and in
the test fixture, and the version table lives in `system.version`. Reference rows load from
`StainlessInfrastructure/.import/*.json`.

Most entities carry a monotonic `version_global`, a per-row `version_local`, or both, and database
triggers maintain the values. Every timestamp is stored in UTC. `published_at` is the editor's date and
the one that the cards order and show; `created_at` says when the row appeared.

### Frontend

- A page inherits `BasePage` and lives in `Frontend/Pages/<Area>/`, split as `X.razor`, `X.razor.cs` and
  `X.razor.css`.
- Each page declares `public const string Url` in its code-behind and routes with
  `@attribute [Route(X.Url)]`. Navigation, redirects and the 404 re-execution all reference those
  constants, so no path is written twice.
- Localization is `en` plus `ja`, through `Frontend/Localization/Resources.resx` and `Resources.ja.resx`
  and a middleware that reads the `tld15-language` cookie.
- Styling is a global `wwwroot/app.css` of CSS custom properties, plus per-component scoped CSS.
- Stored prose is markdown. `MarkdownService` renders it with Markdig, with HTML disabled and link
  schemes limited to `http`, `https` and `mailto`. `MarkdownView` is the only component that hands the
  result to a `MarkupString`. The same service also flattens a text into the meta description, so the
  description is the page's own opening rather than a line kept by hand beside it.
- A set of links is one JSON dictionary of an address to the language it speaks, and it hangs off the
  root row rather than a translation: a profile is the same address whichever language a reader arrives
  in.
- `ExternalLinkIcon` is the one outward link button on the site. It reads the icon off the host of the
  address, and an address it does not recognize draws the placeholder. Nothing else writes an anchor
  around an icon.

### Sessions and authentication

Cookie authentication carries the session, but the session state lives in `CacheManager` in memory, keyed
by a `Session` claim. `CookieEvent.ValidatePrincipal` re-checks every request against that cache: a
missing session, a changed feature set, or changed request metadata signs the user out. API-key hashes
load into the same cache at startup. A restart therefore drops every session, by design.

## Contributing

This project exists for one website, and its roadmap follows what that site needs. A pull request lands
only when an administrator wants it. An issue, a fix or a security note is welcome all the same.

[CONTRIBUTING.md](CONTRIBUTING.md) holds the rules. In short:

- Open an issue first, and wait for an answer before you write code.
- Branch off `main`, and open a pull request into it. A merge into `main` deploys.
- Sign the [Contributor License Agreement](CLA.md) on your first pull request.
- Run `dotnet format` and `dotnet test` before you commit.
- We do not argue about detail. The site must work, stay readable, and keep its security rules.

## License

TLD-15 is open source under the [AGPL-3.0-only](LICENSE). [LICENSING.md](LICENSING.md) holds the map and
the details.

Run it for yourself, for your family, or for your organization, and pay nothing. When you run a modified
copy as a network service for other people, the AGPL asks you to publish your changes. The server answers
`GET /api/public/source` with the address of its source code, and the footer shows the same address.

The license covers the code. The name **Fireplace of Despair**, the division names, the brand, the logo
and the written content of the site stay with the copyright holder. Rename your fork and replace the
content before you publish it.

The AGPL does not fit every company. Fireplace of Despair owns the full copyright, so it can grant other
terms. Write to **ChiefService@outlook.com** with the words `TLD15: license` in the subject.

Copyright (c) 2025 Fireplace of Despair.
