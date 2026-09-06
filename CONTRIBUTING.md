<h1 align="center">Contributing to TLD-15</h1>

<p align="center">
  This project exists for one website, <a href="https://fireplace-of-despair.org/">fireplace-of-despair.org</a>.
  The code is open. A pull request lands only when an administrator wants it.
</p>

---

## Read this first

TLD-15 is an internal project. It serves one organization and one site. We publish the source because
open code is better code, and because the skeleton under it is worth reading. We do not run this repository as a product,
and we owe nobody a review.

Outside help is welcome all the same. A bug report, a fix, a translation, a security note: all of these
are useful, and we read every one of them.

Two rules follow from that.

1. **Open an issue before you write code.** An administrator answers whether we want the change. A
   pull request that arrives without this step can sit unread, or close unmerged. Neither outcome is a
   judgment of your work.
2. **We do not argue about detail.** The site must work, stay readable, and keep its security rules.
   Beyond that we hold no strong opinion. A review comment about a name, a blank line, or a style the
   linter accepts is not worth your time or ours.

## Before you start

1. Install the [.NET 11 SDK](https://dotnet.microsoft.com/download).
2. Set up a PostgreSQL database. See [README.md](README.md#quick-start).
3. Read the rules below for the area that you touch.
4. Run `dotnet format` and `dotnet test` before you push.

<details>
<summary><kbd>Table of contents</kbd></summary>

- [License](#license)
- [Branches and pull requests](#branches-and-pull-requests)
- [Code style](#code-style)
- [Comments](#comments)
- [Project structure](#project-structure)
- [Constants](#constants)
- [Features](#features)
- [Endpoints](#endpoints)
- [Errors: incidents](#errors-incidents)
- [Database: migrations](#database-migrations)
- [Database: versioning](#database-versioning)
- [Database: date and time](#database-date-and-time)
- [Frontend](#frontend)
- [Tests](#tests)
- [Security](#security)
- [What we do not care about](#what-we-do-not-care-about)
- [Pull request checklist](#pull-request-checklist)

</details>

---

## License

The MPL-2.0 license covers this repository. [LICENSING.md](LICENSING.md) holds the details. You
license your contribution under the same license.

Two more files carry a rule that a contributor needs.

- [TRADEMARKS.md](TRADEMARKS.md) states which name, which logo and which content stay with the
  copyright holder. Read it before you publish a fork.
- [NOTICE](NOTICE) lists every third-party component and its license. Add your package to that list
  when your pull request adds a dependency.

Every new source file carries this header. The first three lines are Exhibit A of the MPL. A generated
`*.Designer.cs` file carries no header, because the tool rewrites that file.

```csharp
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)
```

The header is not a formality. The MPL works on each file, and section 3.4 asks every copy to keep the
notice. A file without the header gives a reader no way to find the license.

**A pull request needs a signed Contributor License Agreement.** Read [CLA.md](CLA.md) for the text.
Two facts drive this rule. Shevtsov Stanislav sells a commercial license for a company that cannot
publish a change to a file of this project. The site also mixes code under the MPL with written
content, division names and a brand that stay under normal copyright. Both need one owner for the
copyright of the whole work. The agreement gives Shevtsov Stanislav that right. You keep your own
copyright, and you keep the right to use your own code anywhere.

You sign once. That signature covers every later pull request.

Do not copy code from another project into this one. When you port an idea from a licensed source, name
the source in the pull request, and wait for an answer before you continue.

## Branches and pull requests

**Never commit to `main`.** Branch from `main`, and open the pull request into `main`.

```
your branch --PR--> main --deploy--> the live site
```

A merge into `main` starts `.github/workflows/rollout.yml`, which builds the image and rolls it out to
the server. There is no staging branch and no manual release step. Treat every merge as a deploy.

| Rule | Detail |
|---|---|
| One pull request | One issue. Do not bundle a refactor with a fix. |
| Merge type | Squash. The message starts with the issue number and holds a description. |
| Size | Small. A large diff hides the defect that a reviewer looks for. |

An administrator merges. Nobody else does.

## Code style

`.editorconfig` is the single source of truth.

```bash
dotnet format tld15Server/tld15Server.csproj
```

Every project sets `EnforceCodeStyleInBuild`. A style violation appears during a build. Fix it. Do not
suppress it.

Core rules:

| Rule | Value |
|---|---|
| Indentation | 4 spaces. Never tabs. |
| Line ending | CRLF. A `.sh` file stays LF through `.gitattributes`. |
| Braces | Allman. The opening brace goes on the next line. |
| `using` | Explicit. `ImplicitUsings` is off. `System` first, outside the namespace. |
| Namespace | File-scoped. |
| `var` | Allowed. Use it when the right side names the type. |
| Target-typed `new()` | Use it when the left side names the type. |
| Collection expressions | Use `[...]`. Do not use `new List<T>()`. |
| Final newline | Required. |
| Trailing whitespace | Trimmed. |

Write guard clauses. Return early. Keep the happy path at the lowest indentation.

```csharp
// correct
if (item is null) { return; }
Draw(item);

// wrong
if (item is not null) { Draw(item); }
```

Reformat only the lines that you change. Do not reformat a whole file in a feature pull request.

## Comments

Write a comment only for one of these cases:

- A workaround or a hack.
- A quick fix that a later change must remove.
- A special condition that the code does not show.
- Complex business logic.
- An optimization or a strange decision that a reader will question.

Do not comment self-explanatory code. The code must carry the meaning.

**Exception.** Every public member of `StainlessCore` needs an XML `<summary>` comment. That project
does not suppress `CS1591`.

**Exception.** Every feature carries an XML `<summary>` comment that says what the feature does.

Use the imperative mood: "Gets the value", not "Get the value". Link related types with `<see cref=""/>`.

## Project structure

Four projects plus one test project. `tld15.slnx` holds them.

| Project | Role |
|---|---|
| `StainlessCore` | Shared kernel: `Execute`, incidents, feature and endpoint contracts, hashing, API keys, the login throttle. |
| `StainlessInfrastructure` | EF Core contexts, FluentMigrator migrations, reference-data import. |
| `StainlessGenerators` | The Roslyn generator. It targets `netstandard2.0` and loads as an analyzer. |
| `tld15Server` | The site: Blazor Server UI, Minimal-API endpoints, features, composition. |
| `Tests/tld15ServerTests` | xUnit v3. `tld15Server` grants it `InternalsVisibleTo`. |

The `Stainless*` projects are a shared skeleton. A sibling repository uses the same three. Change one of
them only when the change suits both sides. A change that serves this site alone belongs in
`tld15Server`.

Every project owns two composition files.

```
<project>/Composition/Globals.cs           // constants
<project>/Composition/ServiceInjection.cs  // DI wiring
```

`ServiceInjection.cs` exposes an extension method. `Program.cs` calls it, and the integration tests call
the same method. Keep `Program.cs` thin: it chains extension methods and nothing else.

Avoid reflection-based configuration binding and startup scans. The skeleton stays trim-friendly on
purpose. Read the remarks on `StainlessCore.Composition.ServiceInjection.ReadEntries`.

## Constants

Declare every constant in `Composition/Globals.cs`. Split the contents into nested static classes by
domain. A content id, a cookie name, a claim type, a config key, a route: each one belongs there.

Prefer a meaningful string over a number, because a string reads well in a log.

```csharp
public static class Globals
{
    public static class Route
    {
        public const string Admin = "/admin";
    }

    public static class Content
    {
        public static string Lore => "lore";
    }
}
```

A page declares its own address as `public const string Url` in its code-behind. Never write a path
twice, and never hard-code one in a link.

## Features

A feature is one self-contained action. Put it in one file.

```
tld15Server/Features/<Area>/<Name>Feature.cs
```

The file holds the `Command` or `Query`, the `Result`, and the `Handler` in one class.

```csharp
public sealed class HomeGetFeature : IFeature
{
    public const string Id = "home.get";        // dotted, lowercase
    public static string FeatureId => Id;

    public sealed class Result { }
    public sealed record Query : IQuery<Result> { }

    public sealed class Handler(IDbContextFactory<DataContextBusiness> factory)
        : IQueryHandler<Query, Result> { }
}
```

**Every `IFeature` needs a static `Id` and `FeatureId`.** `StainlessGenerators` finds every feature at
compile time and writes `StainlessGenerated.Registry.FeatureIds`. At startup each id becomes an
authorization policy that asks for a `Feature` claim of the same value. An endpoint references the same
value as its `Metadata.FeatureId`. Adding the class is the whole registration step. Do not add a
reflection scan, and do not keep a list by hand.

A new feature also needs a migration row, because `identity.feature` holds the id and its translations.

The project uses [Mediator](https://github.com/martinothamar/Mediator), a source-generated dispatcher.
Do not wire a handler into DI by hand. Dispatch with `IMediator.Send`.

A handler takes `IDbContextFactory<T>` and creates one `DbContext` per unit of work. Do not inject a
scoped `DbContext`.

Rules for the action name:

| Case | Name | Reason |
|---|---|---|
| Create and update | `PostEntityFeature` | One feature covers both. Use POST. Avoid PUT and PATCH. |
| Create runs automatically | `PutEntityFeature` | The user cannot create the record. Use PUT. |
| Search | `SearchEntityFeature` | A search request grows too large for a query string. Use POST. |
| Read | `GetEntityFeature` | |
| Delete | `DeleteEntityFeature` | |

A `Post` feature replaces its entity **whole**. `ProjectPostFeature` drops a locale that arrives blank,
and `ContentPostFeature` overwrites both the links and the text. An editor that touches one side must
send the other side back exactly as the `Get` feature handed it over.

A shared service between features is fine. A chain of feature calls is fine. Avoid a shared model. When
two features do share one, put it here:

```
tld15Server/Features/_Shared/<Area>/SharedEntity.cs
```

The folder name starts with an underscore, so it sorts to the top. Visual Studio writes `._Shared` into
the namespace. Fix that by hand: the namespace ends with `.Shared`.

## Endpoints

Put an endpoint in the folder that matches its access type.

```
tld15Server/Endpoints/{Public,Protected,External}/
```

| Folder | Interface | Route | Access |
|---|---|---|---|
| `Public` | `IEndpointPublic` | `/api/public` | Anyone can call it. |
| `Protected` | `IEndpointProtected` | `/api/protected` | `ApiKeyFilter` checks the `X-API-KEY` header. A rate-limit policy applies. |
| `External` | `IEndpointExternal` | `/api/external` | Reserved for webhooks and third-party integration. |

An endpoint exposes a static `Metadata` with a `FeatureId`, and a static `ConfigureRouting`. The
generator writes one mapper per interface, and `AddEndpoints` calls them.

`ExceptionHandlingFilter` registers first in every group, so it wraps `ApiKeyFilter`. Keep that order.

## Errors: incidents

Throw `Exception` for an unexpected failure. Throw `IncidentException(IncidentCode.X)` when the caller
can act on the result.

`ExceptionHandlingFilter` catches everything for the API. It logs the real error with Serilog, and it
returns an obfuscated `Incident`. Never let a real exception message reach a client.

A Blazor page does not catch. It wraps the call in `Execute.Run(...)` and reads `Result.Data` or
`Result.IncidentCode`.

| You throw | The caller receives |
|---|---|
| Any exception | `General = 1_000` |
| `IncidentException(IncidentCode.WrongPassword)` | `WrongPassword = 2_000` |

Read `IncidentCode` and `IncidentCodeExtension.ToHTTPCode()` for the full map. A caller branches only on
a specific code.

Swallowing an exception is fine in some cases. Add a comment that states the reason.

## Database: migrations

The server uses FluentMigrator. Put a migration here:

```
StainlessInfrastructure/Migrations/
```

File name:

```
V%yyyy%_%MM%_%dd%_%HHmm%_Short_Description.cs
```

Class name and attribute:

```csharp
[Migration(2026_08_31_1336, "Init: Business")]
public sealed class V2026_08_31_1336_Init_Business : Migration { }
```

`MigrationRunner.Up` applies migrations at application start, and once per test assembly. The version
table lives in `system.version`.

`MigrationHelper` attaches the version triggers and loads reference rows from
`StainlessInfrastructure/.import/*.json`. Those files copy to the output folder.

**EF Core does not own the schema.** Tables, triggers, constraints and seed data come from a migration.

A migration is forward-only in practice. Never edit a migration that reached `main`. Write a new one.

## Database: versioning

One `DbContext` maps to one PostgreSQL schema: `identity`, `reference`, `business`, `archive`,
`settings`. All of them register as `AddDbContextFactory`.

| Interface | Column | Meaning |
|---|---|---|
| `IVersionGlobal` | `version_global` | A monotonic number that increases across all entities. |
| `IVersionLocal` | `version_local` | A per-row number for optimistic concurrency. |
| `IUpdatable` | `created_at`, `updated_at` | UTC row timestamps. |

Attach the trigger in the entity configuration.

```csharp
this.AttachGlobalVersionTrigger("table_name", "schema_name");
this.AttachLocalVersionTrigger("table_name", "schema_name");
```

**Relation-only updates.** A relation table often carries no version. When you change only the links,
EF Core does not touch the parent entity, so the trigger does not fire. Increment the version by hand.

```csharp
account.VersionLocal++;
```

**Validation.** Compare versions in code, never in the database.

## Database: date and time

Store every date and time in UTC. Display local time.

Two exceptions apply.

1. A calendar date, such as a birthday, means the same day in every zone. Use `DateOnly`. Never convert
   it into a user zone. Add a `Local` postfix when the meaning does not read clearly.
2. A date that belongs to one physical place carries the zone in its name, such as `MoveInJST`.

Add a `UTC` postfix when the UTC meaning of a name does not read clearly.

`business.project.published_at` is the editor's date, and the cards order and show it. `created_at`
belongs to the version trigger, and it says when the row appeared.

### Browser time

`BasePage` exposes the browser zone. `InitializeBrowserTime` reads that zone once through JavaScript,
and it sits inside an `AuthorizeView` in `MainLayout`. A signed-out visitor therefore loads no
interactive component, and no circuit opens on a public page.

A break here stays silent. The provider falls back to the zone of the server, and a developer machine
runs both in one zone. Read the network log after a change to this area.

## Frontend

- A page inherits `BasePage` and lives in `Frontend/Pages/<Area>/`. Split it as `X.razor`, `X.razor.cs`
  and `X.razor.css`.
- A page declares `public const string Url` in its code-behind, and routes with
  `@attribute [Route(X.Url)]`.
- **A public page declares no render mode.** `Home` and `ProjectReadPage` are read, not operated, so
  they render statically and ship whole in the first response. Keep it that way. Check the network log
  for `_blazor/negotiate` after a change: a request there means you made a public page interactive.
- Every page that manages the site lives under `Globals.Route.Admin`. That prefix is the one line
  `robots.txt` has to carry.
- Localization is `en` plus `ja`, in `Frontend/Localization/Resources.resx` and `Resources.ja.resx`.
  **Keep both files in sync.** A missing Japanese key falls back to English and looks like a bug.
- Styling is the global `wwwroot/app.css` plus per-component scoped CSS. Scoped CSS cannot reach markup
  that a component did not write itself, so rules for rendered markdown live in `app.css` under
  `.markdown-body`.
- Do not put `scroll-behavior: smooth` on the document. A browser drops a smooth jump of a few thousand
  pixels, and the anchors on the front page stop working.
- **Stored prose is markdown, never HTML.** `MarkdownService` renders it, and `MarkdownView` is the only
  component that hands the result to a `MarkupString`. Do not add a second one.
- **A poster is an address, never an upload.** No editor writes to disk. `UrlPolicy` decides what a
  browser may load or follow, and both `MarkdownService` and the post features ask it.
- A set of links is one JSON dictionary of an address to the language it speaks. It hangs off the root
  row, never off a translation. The address is the key, so two rows on one address are a validation
  error. A blank language is allowed, because the language is a badge.
- `ExternalLinkIcon` is the one outward link button. It owns its own CSS and carries no spacing. The row
  around it sets the gap. Nothing else writes an anchor around an icon.
- Reusable editor parts live in `Frontend/Components/Common`.

## Tests

The repository runs on Microsoft Testing Platform. The root `global.json` selects the runner.

```json
{ "test": { "runner": "Microsoft.Testing.Platform" } }
```

A new test project follows five rules:

1. Reference `xunit.v3.mtp-v2`. Do not reference `xunit` or `xunit.v3`.
2. Set `<OutputType>Exe</OutputType>`. A test project runs itself.
3. Set `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`.
4. Do not add `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or `coverlet.collector`.
5. Every project uses the same runner. Mixing runners fails the run.

### Layout

The test path mirrors the target path.

```
target:            tld15Server/Features/System/PingGetFeature.cs
unit test:         Tests/tld15ServerTests/Application/UnitTests/Features/System/PingGetFeature_Tests.cs
integration test:  Tests/tld15ServerTests/Application/IntegrationTests/Features/System/PingGetFeature_Tests.cs
```

| Folder | Mocks |
|---|---|
| `UnitTests` | Moq. Mock every dependency. |
| `IntegrationTests` | None, or as few as possible. A real database. |

An integration test builds its services through the same `ServiceInjection` extension methods that
production uses.

### The database

Integration tests need a live PostgreSQL. `Tests/tld15ServerTests/appsettings.Tests.json` points at
`localhost:5432` and the database `tld15-local-test`. The assembly fixture in `AssemblyInfo.cs` applies
the migrations before any test runs.

A test class that mutates accounts must not run beside another one that does. `IdentityPostFeature`
creates the first account only when no other account exists. One account left behind suppresses that
path, and every identity test then fails.

### Commands

```bash
dotnet test tld15.slnx
```

```bash
dotnet test Tests/tld15ServerTests/tld15ServerTests.csproj -- --filter-method "*IdentityPostFeature_Tests*"
```

Filters are platform options after `--`, and not `--filter`. `--filter-class`, `--filter-namespace`,
`--filter-trait` and the `--filter-not-*` family work the same way. A filter that matches nothing ends
the run with exit code 8. The test project is an executable, so
`dotnet run --project Tests/tld15ServerTests -- --help` lists every option.

### Writing tests

- Mark a generated test with a comment: `// LLM - <model-name> <model-version>`.
- Keep a test granular. One test covers one behavior.
- Name a test `Subject_Condition_Result`. The name reads as a sentence.

  ```csharp
  ProjectPostFeature_WithBlankLocale_DropsTranslation
  ProjectReadPage_WithUnknownId_Returns404
  ```

- Read the existing tests in the target folder before you add a new one.
- Do not lower coverage.

## Security

Treat these rules as hard limits. A pull request that breaks one does not merge.

- **Never log a secret.** This covers a password, an API key, and a pepper.
- **Never return a real exception to a client.** `ExceptionHandlingFilter` owns the response.
- **Never store a plain API key.** The server stores a hash of the key plus the pepper. The plain key
  appears once, in the response that created it.
- **Never render stored HTML.** `MarkdownService` disables HTML and limits a link to `http`, `https` and
  `mailto`. Do not relax either setting, and do not add a second `MarkupString` call site.
- **Never accept a poster that `UrlPolicy` rejects.** An address reaches a browser. Treat it as input.
- **Never reorder the middleware in `Program.cs`.** `UseForwardedHeaders` runs first, because the
  rate-limit partitions, the session metadata checks and the cookie policy all read the caller address.
  Each step carries a comment that says why it sits there.
- **Never trust `X-Forwarded-For` by default.** Add your proxy to `Security:ForwardedHeaders` when you
  deploy behind one. An open list lets one caller lock out every user.
- **Never commit a secret.** This covers `appsettings.json`, a connection string, and a pepper.
  `.dockerignore` keeps every `appsettings.json` out of the image. Keep it that way.

Report a vulnerability in private. Do not open a public issue for it.

## What we do not care about

This project serves one site. We spend review time on the parts that break the site, and on nothing
else. These points never block a merge:

- A name that reads well enough.
- A blank line, a comment style, or an order of members that the linter accepts.
- A pattern that another repository prefers.
- An abstraction that a change does not need yet. Duplication is cheaper than an early abstraction here.
- A benchmark for a page that one person opens twice a day.

Do not write a long defense of a choice in the pull request. Write what the change does, and why.

## Pull request checklist

- [ ] An issue exists, and an administrator agreed to the change.
- [ ] The Contributor License Agreement carries your signature.
- [ ] Every new source file carries the SPDX header.
- [ ] The branch starts from `main`, and the pull request targets `main`.
- [ ] The change covers one issue. The message names the issue number.
- [ ] `dotnet format` reports no change.
- [ ] `dotnet build` reports no style violation.
- [ ] `dotnet test` passes.
- [ ] A new feature exposes a static `Id` and `FeatureId`, and a migration adds it to the database.
- [ ] A new migration follows the naming rule, and edits no released migration.
- [ ] A new resource key exists in both `Resources.resx` and `Resources.ja.resx`.
- [ ] A public page still declares no render mode.
- [ ] No secret appears in the diff or in a log line.
- [ ] Line endings are CRLF.
