// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the emphasis of a toolbar button.
/// </summary>
public enum ToolbarEmphasis
{
    /// <summary>
    /// The main action of the toolbar - a filled button.
    /// </summary>
    Primary = 0,

    /// <summary>
    /// A regular action - an outlined button.
    /// </summary>
    Secondary = 1,

    /// <summary>
    /// A minor action - a plain button without a frame.
    /// </summary>
    Quiet = 2
}
