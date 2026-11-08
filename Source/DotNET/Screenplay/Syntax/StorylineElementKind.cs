// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The kinds of element a storyline step can name.
/// </summary>
public enum StorylineElementKind
{
    /// <summary>A module.</summary>
    Module,

    /// <summary>A feature, at any nesting depth.</summary>
    Feature,

    /// <summary>A slice.</summary>
    Slice,

    /// <summary>A command.</summary>
    Command,

    /// <summary>An event.</summary>
    Event,

    /// <summary>A read model.</summary>
    ReadModel,

    /// <summary>A query.</summary>
    Query,

    /// <summary>A screen.</summary>
    Screen,

    /// <summary>A reaction.</summary>
    Reaction
}
