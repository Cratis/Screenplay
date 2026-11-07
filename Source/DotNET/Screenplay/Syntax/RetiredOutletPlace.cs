// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Where Screenplay 5 wrote <c>outlet &lt;name&gt;</c> for what Screenplay 6 calls a slot.
/// </summary>
public enum RetiredOutletPlace
{
    /// <summary>A component's <c>outlet &lt;name&gt;</c>.</summary>
    Component = 0,

    /// <summary>A layout's or template's <c>outlet &lt;name&gt;</c>.</summary>
    Template = 1,

    /// <summary>A navigation's <c>outlet &lt;name&gt;</c>.</summary>
    Navigation = 2,

    /// <summary>A contribution's <c>destination outlet &lt;name&gt;</c>.</summary>
    Destination = 3
}
