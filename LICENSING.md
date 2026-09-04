<h1 align="center">Licensing</h1>

<p align="center">
  TLD-15 is open source. One license covers the whole repository.
</p>

---

## The map

| Path | License | SPDX identifier | File |
|---|---|---|---|
| `tld15Server/` | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |
| `StainlessCore/` | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |
| `StainlessInfrastructure/` | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |
| `StainlessGenerators/` | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |
| `Tests/` | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |
| `.deploy/` | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |
| Everything else in the root | GNU Affero General Public License v3.0 only | `AGPL-3.0-only` | [LICENSE](LICENSE) |

Every source file carries the same SPDX header. No folder holds an exception.

Copyright (c) 2025 Fireplace of Despair.

## The reason for the license

TLD-15 runs as a website. A reader reaches it over a network and never receives a binary. Under a
permissive license, a person can run a changed copy of this site as a service and return nothing. The
AGPL closes that route. It keeps the code open in the one place that matters for a web application.

The choice also matches the rest of our work. The `Stainless*` projects are a shared skeleton, and a
sibling repository carries them under the same license.

## What you may do

- Run the site on your own hardware, for any number of your own readers. No fee applies.
- Read, fork, and change every line.
- Use the `Stainless*` skeleton as the base of your own application.
- Study the security rules, and check them against the code.

## What the AGPL asks of you

The AGPL applies to the whole repository. Three cases matter.

1. You run the unchanged site as a service for other people. Point your readers to this repository.
2. You run a **changed** copy as a service for other people. Publish your changes under the AGPL, and
   offer the source to every user of that service.
3. You give a build to another person. Ship the source with it, or offer the source to that person.

The server answers `GET /api/public/source` with the repository address, and the footer shows the same
link. Both read `Application:SourceUrl`. Point that key at your own fork when you deploy a changed copy.
Read section 13 of the [AGPL-3.0-only](LICENSE) for the exact wording.

Your own use never triggers a duty. The AGPL asks for source only when other people reach your build.

## The content and the name

The license covers the code. The license does not cover:

- The name **Fireplace of Despair**, and the names of its divisions.
- The logo, the pictures, and the fonts that the repository carries or points to.
- The written content of the site: the lore, the projects, the articles, and the press entries.

That content belongs to Fireplace of Despair, and it stays under normal copyright. A fork must replace
it. Rename your fork, drop our text, and use your own pictures before you publish it.

This split is the practical point of the whole file. The code is a gift. The identity is not.

## The commercial license

The AGPL does not fit every company. A separate license removes the AGPL conditions. It suits you in
these cases:

- You want to run a changed copy as a service, and you want to keep your changes private.
- You want to embed the code in a closed product.
- Your legal department forbids the AGPL.

Fireplace of Despair owns the full copyright, so Fireplace of Despair can grant this license. The
Contributor License Agreement keeps that route open.

Write to **ChiefService@outlook.com** with the words `TLD15: license` in the subject.

## A contribution

Read the [License](CONTRIBUTING.md#license) section of `CONTRIBUTING.md` before your first pull request.
