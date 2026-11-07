// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// How a module, feature or screen combines its outlet items with the ones it inherits.
/// </summary>
public enum OutletMode
{
    /// <summary>
    /// No mode - the outlet's declaration on a layout or template.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// <c>inherits</c> - the inherited items, unchanged. The explicit form of writing nothing.
    /// </summary>
    Inherit = 1,

    /// <summary>
    /// <c>extends</c> - the inherited items, then the own ones.
    /// </summary>
    Extend = 2,

    /// <summary>
    /// <c>overrides</c> - the own items only.
    /// </summary>
    Override = 3
}
