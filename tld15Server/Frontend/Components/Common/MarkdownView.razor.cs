// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
// SPDX-License-Identifier: MPL-2.0
// SPDX-FileCopyrightText: 2025-2026 Shevtsov Stanislav (Fireplace of Despair)

using Microsoft.AspNetCore.Components;
using tld15Server.Services;

namespace tld15Server.Frontend.Components.Common;

/// <summary>
/// Renders a markdown text. Every page that shows stored prose goes through here, so the preview of
/// the editor and the page a visitor reads cannot drift apart: they render the same source with the
/// same renderer.
/// </summary>
/// <remarks>
/// The html the markup string receives is built by <see cref="MarkdownService"/> and by nothing
/// else. The class of the wrapper is what <c>app.css</c> styles: scoped css does not reach markup a
/// component did not write itself.
/// </remarks>
public partial class MarkdownView
{
    [Inject] private MarkdownService Renderer { get; set; } = default!;
    [Parameter] public string? Markdown { get; set; }

    private string _html = string.Empty;

    protected override void OnParametersSet()
    {
        _html = Renderer.ToHtml(Markdown);
    }
}
