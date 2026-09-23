// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the shape of an <see cref="EventContextMember"/> - what, if anything, a path may address below it.
/// </summary>
public enum EventContextMemberKind
{
    /// <summary>
    /// A single value. A value typed by a concept exposes its underlying <c>value</c>; a primitive exposes nothing.
    /// </summary>
    Value = 0,

    /// <summary>
    /// A composite whose own members a path may continue into, such as <c>eventType.id</c>.
    /// </summary>
    Composite = 1,

    /// <summary>
    /// A collection. There is no addressing grammar for its elements, so a path never continues below it.
    /// </summary>
    Collection = 2,

    /// <summary>
    /// A derived function evaluated against the value the rest of the path resolves to, such as <c>occurred.Week</c>.
    /// It is always the last segment of a path.
    /// </summary>
    Function = 3
}
