// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines how a hook - a button linked to a State Change slice - opens its slice when the author names it.
/// </summary>
/// <remarks>
/// A hook without one opens automatically: the slice's first <see cref="ScreenFit"/> the page has free.
/// </remarks>
public enum HookOpening
{
    /// <summary>
    /// The hook issues the slice's command directly, without a form - written <c>runs</c>.
    /// </summary>
    Runs = 0,

    /// <summary>
    /// The hook opens the slice's screen in a dialog - written <c>opens dialog</c>.
    /// </summary>
    Dialog = 1,

    /// <summary>
    /// The hook opens the slice's screen in a drawer - written <c>opens drawer</c>.
    /// </summary>
    Drawer = 2,

    /// <summary>
    /// The hook opens the slice's screen in a named slot - written <c>opens in slot &lt;name&gt;</c>.
    /// </summary>
    Slot = 3
}
