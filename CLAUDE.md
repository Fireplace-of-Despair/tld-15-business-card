# CLAUDE.md

Guidance for Claude Code (claude.ai/code) in this repository.

## What this is

TLD-15 is the website of Fireplace of Despair: the lore, the projects, the articles, the press and the
contacts. It is Blazor Server on .NET 11 preview, with PostgreSQL, Serilog and the MPL-2.0
license. The `Stainless*` projects are a shared skeleton, and a sibling repository uses them.
`tld15Server` is the site.

Three files hold the rest. [README.md](README.md) explains the site. [CONTRIBUTING.md](CONTRIBUTING.md)
holds the full rules for code. [.deploy/README.md](.deploy/README.md) holds the deployment.

## Commands

```bash
dotnet build tld15.slnx
```

```bash
dotnet run --project tld15Server
```

The dev profile serves `http://localhost:5105`.

```bash
dotnet test tld15.slnx
```

`global.json` selects the Microsoft.Testing.Platform runner. Filters are platform options after `--`,
and not `--filter`.

```bash
dotnet test Tests/tld15ServerTests/tld15ServerTests.csproj -- --filter-method "*IdentityPostFeature_Tests*"
```

`--filter-class`, `--filter-namespace`, `--filter-trait` and `--filter-not-*` work the same way. The
test project is an executable, so `dotnet run --project Tests/tld15ServerTests -- --help` lists every
option.

```bash
docker build -f tld15Server/Dockerfile -t tld15-server:latest .
```

Build the image from the repository root. Every `COPY` line reads from there.

### Gotchas

- A running dev server locks `bin/Debug/net11.0/*.dll`, and the build then fails with MSB3027. Stop the
  application first, or build `-c Release` into a separate output.
- Integration tests need a live PostgreSQL. `appsettings.Tests.json` points at `localhost:5432` and the
  database `tld15-local-test`. The assembly fixture applies the migrations before any test.

## Architecture

| Project | Role |
|---|---|
| `StainlessCore` | Shared kernel: `Execute`, incidents, feature and endpoint contracts, hashing, API keys, login throttle. `CS1591` stays on, so every public member needs a comment. |
| `StainlessInfrastructure` | EF Core contexts, FluentMigrator migrations, reference-data import. |
| `StainlessGenerators` | The Roslyn generator. It targets `netstandard2.0` and loads as an analyzer. |
| `tld15Server` | The site: Blazor Server UI, Minimal-API endpoints, features, composition. |
| `Tests/tld15ServerTests` | xUnit v3. `tld15Server` grants it `InternalsVisibleTo`. |

### Features

Every unit of work is one file under `tld15Server/Features/<Area>/<Name>Feature.cs`. The file implements
`IFeature` and holds everything that the action needs.

```csharp
public class HomeGetFeature : IFeature
{
    public const string Id = "home.get";        // dotted, lowercase
    public static string FeatureId => Id;

    public sealed class Result { ... }
    public sealed record Query : IQuery<Result> { ... }   // Mediator; ICommand for a write
    public sealed class Handler(IDbContextFactory<DataContextBusiness> factory)
        : IQueryHandler<Query, Result> { ... }
}
```

`Id` is also the authorization policy name and the claim value. `StainlessGenerators` finds every
`IFeature` at compile time and writes `StainlessGenerated.Registry.FeatureIds`. Startup turns each id
into a policy that asks for a `Feature` claim. Adding the class is the whole registration step. Never
add a reflection scan or a list by hand.

A `Post` feature replaces its entity whole. A page that edits one side of an entity sends the other side
back exactly as the `Get` feature handed it over.

### Endpoints

`tld15Server/Endpoints/{Public,Protected,External}/…` implement `IEndpointPublic`, `IEndpointProtected`
or `IEndpointExternal`. Each one exposes a static `Metadata` that carries the feature id, and a static
`ConfigureRouting`. The generator writes `Registry.MapPublic`, `MapProtected` and `MapExternal`, and
`ServiceInjection.AddEndpoints` maps them onto `/api/public`, `/api/protected` and `/api/external`.

A protected route sits behind `ApiKeyFilter`, which reads `X-API-KEY` and compares a hash. A rate-limit
policy applies. `ExceptionHandlingFilter` registers first, so it wraps the key check.

### Errors

A feature throws `IncidentException(IncidentCode)`. A Blazor page wraps the call in `Execute.Run(...)`
and reads `Result.Data` or `Result.IncidentCode`. It does not catch. `ExceptionHandlingFilter` does the
same translation for the API. `IncidentCode` values stay obfuscated on purpose, and
`IncidentCodeExtension.ToHTTPCode` maps them to HTTP codes.

### Middleware order

The order in `tld15Server/Program.cs` is load-bearing, and each step carries a comment that says why.
`UseForwardedHeaders` runs first, because the rate-limit partitions, the session metadata checks and the
cookie policy all read the caller address. `UseRateLimiter` runs next. `UseAuthentication` and
`UseAuthorization` are called by hand, so they land behind the forwarded headers. Do not reorder.

### Data access

One `DbContext` maps to one PostgreSQL schema: `identity`, `reference`, `business`, `archive` and
`settings`. All of them register as `AddDbContextFactory`. A handler creates a context per unit of work,
and injects no context.

**EF Core does not own the schema.** Tables, triggers, constraints and seed data come from FluentMigrator
classes in `StainlessInfrastructure/Migrations`. A file is named `V<yyyy>_<MM>_<dd>_<HHmm>_<Name>.cs`,
with a matching `[Migration(2026_08_31_1336, "Init: Business")]`. `MigrationRunner.Up` runs at
application start and in the test fixture. The version table lives in `system.version`. `MigrationHelper`
attaches the `version_local` and `version_global` triggers, and loads reference rows from
`StainlessInfrastructure/.import/*.json`.

`business.project.published_at` is the editor's date, and the cards order and show it. `created_at`
belongs to the version trigger, and it says when the row appeared.

### Sessions, authentication and cache

Cookie authentication uses the cookie `tld15-main`, but the session state lives in `CacheManager` in
memory, keyed by a `Session` claim. `CookieEvent.ValidatePrincipal` checks every request against that
cache. A missing session, a changed feature set, or changed request metadata signs the user out.
API-key hashes load into the same cache at startup. A restart therefore drops every session, by design.
`CacheManager` carries a `TODO` about a refactor.

## The site

### Pages

- A page inherits `BasePage`, which gives it the mediator, the localizer, a cancellation token `_cts`,
  `IncidentCode`, `IsLoading`, `HasFeatureAsync` and the browser time. A page lives in
  `Frontend/Pages/<Area>/` and splits into `X.razor`, `X.razor.cs` and `X.razor.css`.
- A page declares `public const string Url` in its code-behind, and routes with
  `@attribute [Route(X.Url)]`. Navigation, a redirect and the 404 re-execution all read those constants.
  Never hard-code a path.
- **A public page declares no render mode.** `Home` and `ProjectReadPage` are read, not operated, so the
  first response carries every block. A visitor opens no circuit, because `InitializeBrowserTime` and
  `Navigation` sit inside an `AuthorizeView` in `MainLayout`. Check for `_blazor/negotiate` in the
  network log to prove it.
- Every page that manages the site lives under `Globals.Route.Admin`. That prefix is the one line that
  `robots.txt` has to carry. `MainLayout.IsAdmin` hides `GlobalNavigation` there, because those pages
  carry their own navigation at the side.
- `ProjectReadPage` (`/projects/{id}`) is the public page for a project and for an article. It sets the
  canonical link from `Application:Host`, the OpenGraph and `article:*` meta, and a `schema.org` block as
  `application/ld+json`. An id that the table does not carry answers a real 404. An id in
  `Globals.Project.IdReserved` is refused, because those words are segments of the admin routes.

### Content

- **Stored prose is markdown, never HTML.** `content_translation.markdown` holds the source.
  `Services/MarkdownService` renders it with Markdig, with HTML disabled and links limited to `http`,
  `https` and `mailto`. It caches the rendered HTML by source. `Components/Common/MarkdownView` is the
  only component that hands the result to a `MarkupString`. Styling lives in `app.css` under
  `.markdown-body`, because scoped CSS cannot reach markup that a component did not write itself.
- **A poster is an address, never an upload.** No editor writes to disk. `UrlPolicy` decides what a
  browser may load or follow, and both `MarkdownService` and the post features ask it.
- A content carries a poster of its own in `business.content.poster_url`, with the description in
  `content_translation.poster_alt`. Only the lore block on `Home` draws it. The picture stands left of
  the prose, narrows with the window, crops to the height of the block, and disappears below the narrow
  breakpoint.
- A stored set of links is one JSON dictionary of an address to the language that it speaks
  (`Features/_Shared/Business/SharedLink`). It lives in `business.project.links_json` and
  `business.content.links_json`, always on the root row and never on a translation. A profile is one
  address in every language, and the language is a badge that the row carries. The address is the key,
  because it is unique and because the icon follows from it. Two rows on one address are a validation
  error. A blank language is allowed.
- A content editor splits by what the content carries. `ContentLinkEditPage` edits the table of links
  (`Globals.Content.LinkEditable`). `ContentTextEditPage` edits the markdown body and the poster
  (`Globals.Content.TextEditable`).
- Projects and articles share one table and split by `project_type_id`. `ProjectEditPage`
  (`/projects/edit/{id?}`) creates and edits one. The id, the type, the division, the publication date
  and the poster address sit above the locale tabs. The titles, the poster text and the markdown body
  sit inside them. `ProjectPostFeature` drops a locale that arrives blank.
- The press is its own table (`business.press` and `press_translation`), and not a content. A mention
  carries an outward address, a poster and a date, and no body. `PressPage` (`/press`) draws it with the
  same `SharedCard`. The editor lives under `Globals.Route.Admin`.
- A work of `Globals.Divisions.ACD` stays off the front page, and `ArchivePage` (`/archive`) shows it.
  `SharedProjectQuery` holds the columns, the order and the mapping that both walls share. A page only
  chooses which works to ask for.

### Components

- `SharedCardPreview` fills every card. An entry with an `ExternalUrl` leads off the site, and the card
  then opens a new tab and hands it nothing. An entry without one leads to `ProjectReadPage`. An entry
  with no `DivisionId` draws no badge. An entry with no `ProjectTypeId` draws no link buttons.
- `ExternalLinkIcon` is the one outward link button, and it owns its CSS. It takes an address and an
  optional language. `IconHelper.GetIconByUrl` reads the icon off the host, and it also knows `mailto:`
  and a `/rss` path. An address that it does not know draws the placeholder. A missing, blank or invalid
  language draws no badge. The button carries no spacing, and the row around it sets the gap. Every
  outward link goes through it. Nothing else writes an anchor around an icon.
- `GlobalNavigation` is the row under the brand in `MainLayout`: Home, Press and Archive, with the
  current page marked `active`.
- `LocalNavigation` is the row of anchors at the top of `Home`, one per block that the page draws. The
  ids come from `Globals.Content.*` for a block that stands for a content, and from `Globals.Anchor.*`
  for the rest. Do not put `scroll-behavior: smooth` on the document. A browser drops a smooth jump of a
  few thousand pixels, and the anchors then stop working.
- Reusable editor parts live in `Frontend/Components/Common`: `LanguageTabs` marks an empty locale with
  〇, `MarkdownEditor` puts the source next to the preview, and `MarkdownView` renders.

### Feeds and files

- `/sitemap.xml` is built once, at start. `Program.BuildTheSitemap` writes it into the `SitemapService`
  singleton out of `Application:Host`, the front page, and one address per work that carries a
  translation. A work published later appears on the next start. Without `Application:Host` the route
  answers 404, and it never serves a relative address.
- `/robots.txt` is composed, and it is not a file. `RobotsService` maps beside the sitemap. The
  `Sitemap:` directive needs an absolute address, and only `Application:Host` knows one. A file in
  `wwwroot` would hold a second copy of the host and go stale. Without the setting the directive stays
  out. `Globals.Route` holds `Admin`, `Identity`, `Sitemap` and `Robots`, so the routes and the file
  cannot drift apart.
- `/rss` is RSS 2.0, and it reads per request. `RssGetFeature` reads the works, `RssService` writes the
  XML, and `Program.WriteTheFeed` composes it. A reader polls a feed once, so an announcement must not
  wait for the next deploy. The document uses `XDocument`, because the format is a handful of elements,
  and a syndication library would buy a dependency and a reflection surface for forty lines of XML. The
  feed carries the newest `RssGetFeature.MaxItems` works, skips a work with no translation, and picks a
  title the way `ProjectReadPage` does. `App.razor` links it for autodiscovery. The renderer writes the
  plus of the media type as `&#x2B;`, which every parser decodes.

### Localization and styling

Localization is `en` plus `ja`, through `Frontend/Localization/Resources.resx` and `Resources.ja.resx`.
Keep both files in sync. `LocalizationMiddleware` sets the culture from the `tld15-language` cookie.
`InvariantGlobalization` is off, and the satellite languages are pinned to `en;ja`. The Docker runtime
uses the `-extra` tag, so ICU and tzdata are present.

Styling is the global `wwwroot/app.css`, which holds the CSS custom properties, the dark palette and the
`Park Lane NF` display font, plus per-component scoped CSS. `Frontend/Components/Icons/*.razor` are
inline SVG files.

## Conventions

- Every `.cs` file starts with the five-line MPL header: Exhibit A of the MPL-2.0, then
  `// SPDX-License-Identifier: MPL-2.0` and `// SPDX-FileCopyrightText: ...`. `LICENSING.md` holds the
  exact block. A generated `*.Designer.cs` file carries none.
- `ImplicitUsings` is off. Write explicit `using` directives, `System` first, outside the namespace.
- File-scoped namespaces. CRLF and 4 spaces. A `.sh` file stays LF through `.gitattributes`.
- `EnforceCodeStyleInBuild` is on for every project, and `.editorconfig` is the source of truth.
- A magic string belongs in `tld15Server/Composition/Globals`. This covers a content id, a cookie name, a
  claim type, a config key, a route and page metadata.
- Avoid reflection-based configuration binding and startup scans. The skeleton stays trim-friendly on
  purpose. Read the remarks on `StainlessCore.Composition.ServiceInjection.ReadEntries`.

## Deployment

`.github/workflows/rollout.yml` fires on a push to `main`. It builds the image from the repository root
as `tld15-server:latest`. That name is what `.deploy/docker-compose.yml` starts, and it stays lowercase
because Docker refuses a capital letter in a repository name. The workflow then copies the gzipped
tarball to the server over SSH, loads it, reloads Docker Compose, and waits for the `HEALTHCHECK` of the
container to report healthy before it prunes the image that it replaced. Nothing in the roll-out stops
the host. A failed verification leaves the replaced image in place.

`.deploy/` holds the four server-side files: `docker-compose.yml`, `Caddyfile.example`,
`address-lists.sh` and `appsettings.example.json`. `.deploy/README.md` walks a bare virtual machine to a
running HTTPS site.

Secrets live only in the `appsettings.json` of the server. This covers `Security:Pepper` and the
connection string. `.dockerignore` keeps every `appsettings.json` out of the image.

Adding a project to the solution means adding it to both Dockerfile stages. The restore stage copies the
`.csproj`, and the publish stage copies the sources.
