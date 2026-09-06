<h1 align="center">Licensing</h1>

<p align="center">
  TLD-15 is open source. One license covers the whole repository.
</p>

---

## The map

One license covers the whole repository: every project, every test project, and every file in the
root. The license is the **Mozilla Public License 2.0**, SPDX identifier `MPL-2.0`.
[LICENSE](LICENSE) holds the full text.

Every source file carries the same header. No folder holds an exception. The first three lines are
Exhibit A of the [MPL-2.0](LICENSE). Section 3.4 asks every copy to keep them. The two SPDX lines let
a tool read the license and the copyright.

```csharp
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)
```

Copyright (c) 2025-2026 Shevtsov Stanislav ("Fireplace of Despair").

**Shevtsov Stanislav** is the copyright holder. "Fireplace of Despair" is a trade name, not a
separate company.

## The reason for the license

The MPL protects each file of this project, and it stops there. A person can put one class of this
code into a closed product. A person cannot take a file of this project, change it, and hide the
change. The change comes back to every user of that build.

The `Stainless*` projects are the reason for the choice. They are a shared skeleton: the feature
contract, the endpoint contract, the incident codes, the migration runner, the API keys. A sibling
repository already carries them. A person who wants that skeleton under a closed product must not
face a license that reaches the whole product. The MPL reaches the file, and no further.

The site itself asks for no stronger rule. A reader of TLD-15 receives a page, not a build. The value
of this repository is the skeleton and the security rules, and both stay readable under the MPL.

## What you may do

- Run the site on your own hardware, for any number of your own readers. No fee applies.
- Run a changed copy as a service for other people. The MPL asks for no source in this case.
- Read, fork, and change every line.
- Use the `Stainless*` skeleton as the base of your own application.
- Put this code, or one file of it, into a closed product.
- Study the security rules, and check them against the code.

## What the MPL asks of you

The MPL applies to each file. Three cases matter.

1. You change a file of this project. Publish the changed file under the MPL. Your own new files stay
   under your own terms.
2. You give out a build. Tell the person how to get the source of every file that comes from this
   project. Section 3.2 of the [MPL-2.0](LICENSE) holds the exact wording.
3. You copy a file into your own product. Keep the SPDX header, the copyright line and the license
   notice. Section 3.4 asks for this.

The MPL asks for nothing else. Your own code stays yours.

## What the MPL does not ask of you

- The MPL puts no duty on your own new files. You license them as you want.
- The MPL puts no duty on a network service. A hosted build is not a distribution.
- The MPL asks for no notice to the copyright holder.

The server still answers `GET /api/public/source` with the address of its source code, and the footer
shows the same link. Both read `Application:SourceUrl`. The license no longer asks for this. A reader
of a hosted build cannot read the code that runs, so the site offers the address of it. Point that
key at your own fork when you deploy a changed copy.

## A larger work

You may put this code into a larger work, and you may license that larger work under your own terms.
The files of this project keep the MPL. Section 3.3 holds the rule.

This project adds no "Incompatible With Secondary Licenses" notice. A person may therefore combine
this code with a work under the GNU GPL, the GNU LGPL or the GNU AGPL, and give out the combination
under that license.

## The commercial license

The MPL fits almost every case. One case stays.

- You change a file of this project, and you want to keep that change private.

A commercial license removes that condition. Shevtsov Stanislav owns the full copyright, so Shevtsov
Stanislav can grant this license. The Contributor License Agreement keeps that route open.

Write to **chief@fireplace-of-despair.org** with the words `TLD15: license` in the subject.

## The content and the name

The license covers the code. Section 2.3 of the MPL grants no right to a trademark. The license
therefore does not cover:

- The name **Fireplace of Despair**, and the names of its divisions.
- The logo, the pictures, and the fonts that the repository carries or points to.
- The written content of the site: the lore, the projects, the articles, and the press entries.

That content belongs to the copyright holder, and it stays under normal copyright. A fork must
replace it. Rename your fork, drop our text, and use your own pictures before you publish it. Read
[TRADEMARKS.md](TRADEMARKS.md) for the rules.

This split is the practical point of the whole file. The code is a gift. The identity is not.

## Third-party components

[NOTICE](NOTICE) lists every third-party component and its license. Each one keeps its own license
and its own copyright.

## A contribution

Read the [License](CONTRIBUTING.md#license) section of `CONTRIBUTING.md` before your first pull
request.
