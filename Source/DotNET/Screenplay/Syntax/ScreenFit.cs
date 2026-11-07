// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines a place a slice's screen fits - written <c>fits &lt;role&gt;, …</c> in order of preference.
/// </summary>
public enum ScreenFit
{
    /// <summary>
    /// The screen fits a dialog over the page.
    /// </summary>
    Dialog = 0,

    /// <summary>
    /// The screen fits a drawer over the page.
    /// </summary>
    Drawer = 1,

    /// <summary>
    /// The screen fits a slot with the <see cref="SlotRole.Pane"/> role.
    /// </summary>
    Pane = 2,

    /// <summary>
    /// The screen fits a slot with the <see cref="SlotRole.Panel"/> role.
    /// </summary>
    Panel = 3,

    /// <summary>
    /// The screen fits a slot with the <see cref="SlotRole.Page"/> role.
    /// </summary>
    Page = 4
}
