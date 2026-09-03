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
- Stored prose is **markdown**, never html. `content_translation.markdown` holds the source;
  `Services/MarkdownService` renders it (Markdig, `DisableHtml`, link schemes limited to
  http/https/mailto, rendered html cached by source) and `Components/Common/MarkdownView` is the only
  component that hands the result to a `MarkupString`. Styling lives in `app.css` under
  `.markdown-body` — scoped css cannot reach markup a component did not write itself.
- Content editors are split by what a content carries: `ContentLinkEditPage` for the table of links
  (`Globals.Content.LinkEditable`), `ContentTextEditPage` for the markdown body and the poster
  (`Globals.Content.TextEditable`). `ContentPostFeature` replaces a content **whole**, so a page that
  edits one side sends the other side back exactly as `ContentGetFeature` handed it over.
- A content carries a poster of its own (`business.content.poster_url`, description in
  `content_translation.poster_alt`, migration `V2026_09_02_1500_Added_Content_Poster`) — an address
  like every other poster, never an upload. Only the lore block on `Home` draws it: the picture
  stands to the left of the prose, narrows with the window, is cropped to the height of the block,
  and leaves altogether below the narrow breakpoint.
- Projects and articles are the same table, split by `project_type_id`. `ProjectEditPage`
  (`/projects/edit/{id?}`) creates and edits one: the id, type, division, publication date and
  poster address sit above the locale tabs, the title/subtitle/poster text and the markdown body
  inside them. `ProjectPostFeature` replaces a project whole, dropping any locale left blank.
- The press is its own table (`business.press` + `press_translation`, migration
  `V2026_09_02_1200_Init_Press`), not a content: a mention carries an outward address, a poster, a
  `published_at` of its own and no body. `PressPage` (`/press`) draws it with the same `SharedCard`,
  and the editor lives at `Globals.Route.Admin` + `/press/…`.
- `SharedCardPreview` fills every card. An entry with an `ExternalUrl` leads off the site (the card
  then opens a new tab and hands it nothing); one without leads to `ProjectReadPage`. An entry with
  no `DivisionId` draws no badge, and one with no `ProjectTypeId` draws no link buttons.
- `ExternalLinkIcon` (`Frontend/Components`) is the one outward link button, and it owns its own
  css. It takes an address and, optionally, a language: `IconHelper.GetIconByUrl` reads the icon off
  the host (`_hosts`, plus `mailto:` and a `/rss` path), and an unrecognised address draws the
  placeholder icon. A missing, blank or invalid language draws no badge at all. The button carries
  no spacing — the row around it (`.reading-links`, `.project-buttons-container`, `.social-container`)
  sets the gap. Every outward link on the site goes through it: `SharedCard`, `LinkButtons`, and the
  social and contacts blocks on `Home`. Nothing else writes an `<a>` around an icon.
- A stored set of links is one json dictionary of **an address to the language it speaks**
  (`Features/_Shared/Business/SharedLink`), kept in `business.project.links_json` and in
  `business.content.links_json`. Both hang off the **root row**, never off a translation: a profile
  is the same address whichever language a reader arrives in, and the language a link speaks is a
  badge the row carries rather than the locale it lives in. The address is the key because it is
  unique on its own and because the icon follows from it: nothing stores the name of a site, and
  neither editor offers a choice of icon — the cell draws `IconHelper.GetIconByUrl` of whatever
  address the row carries. Two rows on the same address are a validation error rather than a silent
  overwrite, and a language is a badge only, so a blank one is allowed.
- `GlobalNavigation` (`Frontend/Navigations`) is the row under the brand in `MainLayout`: Home,
  Press and Archive, with the current page marked `active`. `MainLayout.IsAdmin` keeps it off the pages under
  `Globals.Route.Admin`, which carry their own navigation at the side.
- Works of `Globals.Archive.DivisionId` (ACD) are kept **off** the front page and shown on
  `ArchivePage` (`/archive`) instead — the same wall of cards, leading to the same
  `ProjectReadPage`. `SharedProjectQuery` holds the columns, the order and the mapping both walls
  share; a page only chooses which works to ask for.
- `LocalNavigation` (`Frontend/Navigations`) is the row of anchors at the top of `Home`, one per
  block the page actually renders. The ids it jumps to are `Globals.Content.*` for the blocks that
  stand for a content and `Globals.Anchor.*` for the rest. Do not put `scroll-behavior: smooth` on
  the document: a browser drops a smooth jump of a few thousand pixels and the anchors stop working.
- `/sitemap.xml` is built **once, at start** (`Program.BuildTheSitemap` into the `SitemapService`
  singleton) out of `Application:Host`, the front page and one address per work that carries a
  translation. A work published later appears on the next start. Without `Application:Host` the
  route answers 404 rather than serving relative addresses.
- Every page that manages the site lives under `Globals.Route.Admin` (`/admin/…`), which is the one
  line `robots.txt` has to carry. `InitializeBrowserTime` and `Navigation` sit inside an
  `AuthorizeView` in `MainLayout`, so a visitor loads no interactive component at all and no circuit
  is opened for a public page — verified by there being no `_blazor/negotiate` on one.
- The public pages — `Home` and `ProjectReadPage` — declare **no render mode**. Nothing on them is
  operated, so they render statically and the first response carries every block. `MarkdownService`
  also flattens a text into the meta description (`ToSummary`), so the description is the page's own
  opening rather than a line kept by hand beside it.
- `ProjectReadPage` (`/projects/{id}`) is the public page for both types and the one the cards link
  to. It declares **no render mode**: a page that is read, not operated, ships whole in the first
  response. It sets the canonical link from `Application:Host` (falling back to the request), the
  OpenGraph and `article:*` meta, a schema.org block as `application/ld+json`, and a real **404**
  status for an id the table does not carry — a friendly message under a 200 is a soft 404. Ids in
  `Globals.Project.IdReserved` are refused, because they are literal segments of the admin routes.
- **Posters are addresses, never uploads.** Nothing is written to disk by an editor; `UrlPolicy`
  decides what a browser may load or follow, and both `MarkdownService` and the post features ask it.
- `business.project.published_at` is the editor's date and the one the cards order and show;
  `created_at` belongs to the version trigger and says when the row appeared.
- Reusable editor parts live in `Frontend/Components/Common`: `LanguageTabs` (one tab per locale,
  marking an empty one with 〇), `MarkdownEditor` (source next to preview), `MarkdownView`.
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

`.github/workflows/rollout.yml` fires on push to `main`: it builds the image **from the repository
root** as `tld15-server:latest` — the name `.deploy/docker-compose.yml` starts, and lowercase
because Docker refuses a capital letter in a repository name — rsyncs the gzipped tarball to the
server over SSH, loads it, reloads Docker Compose, then waits for the container's own `HEALTHCHECK`
to report healthy before pruning the image it replaced. Nothing in the roll-out stops the host, and
a failed verification leaves the replaced image in place to fall back to. `.deploy/` holds the four
server-side files
(`docker-compose.yml`, `Caddyfile.example`, `address-lists.sh`, `appsettings.example.json`) and
`.deploy/README.md` walks a bare VM to a running HTTPS site. Secrets (`Security:Pepper`, connection
string) live only in the server's `appsettings.json` — `.dockerignore` keeps every `appsettings.json`
out of the image. Adding a project to the solution means adding it to **both** Dockerfile stages
(restore copies the `.csproj`, publish copies the sources).
