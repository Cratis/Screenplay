// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The kinds of item an <see cref="OutletSyntax"/> holds.
/// </summary>
public enum OutletKind
{
    /// <summary>
    /// Buttons and groups of buttons - <c>outlet &lt;name&gt; of actions</c>.
    /// </summary>
    Actions = 0,

    /// <summary>
    /// Filters that set parameters of the screen's query - <c>outlet &lt;name&gt; of filters</c>.
    /// </summary>
    Filters = 1,

    /// <summary>
    /// Tabs that show screens in a slot - <c>outlet &lt;name&gt; of tabs</c>.
    /// </summary>
    Tabs = 2,

    /// <summary>
    /// Figures read from single-result queries - <c>outlet &lt;name&gt; of stats</c>.
    /// </summary>
    Stats = 3,

    /// <summary>
    /// Navigation items that target modules and features - <c>outlet &lt;name&gt; of navigation</c>.
    /// </summary>
    Navigation = 4
}
