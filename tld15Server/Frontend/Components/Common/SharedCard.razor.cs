// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2025 Fireplace of Despair

using Microsoft.AspNetCore.Components;
using tld15Server.Features.Shared.Business;

namespace tld15Server.Frontend.Components.Common;

public partial class SharedCard
{
    [Parameter]
    public required ISharedCardContent SharedCardContent { get; set; }

    internal string _url = string.Empty;

    protected override void OnParametersSet()
    {
        if (SharedCardContent is ISharedProjectPreview)
        {
            _url = $"{Pages.Account.AccountReadPage.Url}/{SharedCardContent.Id}";
            return;
        }
        if (SharedCardContent is ISharedArticlePreview)
        {
            _url = $"{Pages.Account.AccountReadPage.Url}/{SharedCardContent.Id}";
            return;
        }
    }
}

