// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Where an outlet item stands next to the item an <see cref="OutletPositionSyntax"/> names.
/// </summary>
public enum OutletPlacement
{
    /// <summary>
    /// <c>before &lt;id&gt;</c>.
    /// </summary>
    Before = 0,

    /// <summary>
    /// <c>after &lt;id&gt;</c>.
    /// </summary>
    After = 1
}
