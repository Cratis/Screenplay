// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the types of slices.
/// </summary>
public enum SliceType
{
    /// <summary>
    /// A slice that accepts a command and appends events.
    /// </summary>
    StateChange = 0,

    /// <summary>
    /// A slice that projects events into a queryable read model.
    /// </summary>
    StateView = 1,

    /// <summary>
    /// A slice that reacts to events and produces side effects.
    /// </summary>
    Automation = 2,

    /// <summary>
    /// A slice that translates external data into events.
    /// </summary>
    Translate = 3
}

/// <summary>
/// Represents a <c>slice</c> declaration - the vertical unit of one behavior.
/// </summary>
/// <param name="Type">The <see cref="SliceType"/> of the slice.</param>
/// <param name="Name">The name of the slice.</param>
/// <param name="Events">The <see cref="EventSyntax">events</see> declared in the slice.</param>
/// <param name="Commands">The <see cref="CommandSyntax">commands</see> declared in the slice.</param>
/// <param name="Queries">The <see cref="QuerySyntax">queries</see> declared in the slice.</param>
/// <param name="Projection">The optional <see cref="ProjectionSyntax"/> declared in the slice.</param>
/// <param name="Captures">The <see cref="CaptureSyntax">captures</see> declared in the slice.</param>
/// <param name="Reactors">The <see cref="ReactorSyntax">reactors</see> declared in the slice.</param>
/// <param name="Screens">The <see cref="ScreenSyntax">screens</see> declared in the slice.</param>
/// <param name="Constraints">The <see cref="ConstraintSyntax">constraints</see> declared in the slice.</param>
/// <param name="Specifications">The <see cref="SpecificationSyntax">specifications</see> declared in the slice.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SliceSyntax(
    SliceType Type,
    string Name,
    IEnumerable<EventSyntax> Events,
    IEnumerable<CommandSyntax> Commands,
    IEnumerable<QuerySyntax> Queries,
    ProjectionSyntax? Projection,
    IEnumerable<CaptureSyntax> Captures,
    IEnumerable<ReactorSyntax> Reactors,
    IEnumerable<ScreenSyntax> Screens,
    IEnumerable<ConstraintSyntax> Constraints,
    IEnumerable<SpecificationSyntax> Specifications,
    SourceLocation Location) : SyntaxNode(Location);
