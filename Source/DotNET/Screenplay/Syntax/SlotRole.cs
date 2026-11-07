// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the role a slot of a layout, screen template or dialog template plays.
/// </summary>
public enum SlotRole
{
    /// <summary>
    /// The slot holds a whole page.
    /// </summary>
    Page = 0,

    /// <summary>
    /// The slot holds a pane beside the page - a detail or a side view.
    /// </summary>
    Pane = 1,

    /// <summary>
    /// The slot holds a panel within the page.
    /// </summary>
    Panel = 2
}
