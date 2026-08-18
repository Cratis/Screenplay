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
/// <param name="Projections">The <see cref="ProjectionSyntax">projections</see> declared in the slice.</param>
/// <param name="Captures">The <see cref="CaptureSyntax">captures</see> declared in the slice.</param>
/// <param name="Reactions">The <see cref="ReactionSyntax">reactions</see> declared in the slice.</param>
/// <param name="Screens">The <see cref="ScreenSyntax">screens</see> declared in the slice.</param>
/// <param name="Constraints">The <see cref="ConstraintSyntax">constraints</see> declared in the slice.</param>
/// <param name="Specifications">The <see cref="SpecificationSyntax">specifications</see> declared in the slice.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional description of the slice.</param>
/// <param name="ReadModels">The <see cref="ReadModelSyntax">read models</see> declared in the slice.</param>
/// <param name="Reducers">The <see cref="ReducerSyntax">reducers</see> declared in the slice.</param>
/// <remarks>
/// A slice declares as many projections as the behavior needs. The read model a screen binds to and the
/// one a command reads to decide belong to the same behavior, so they belong to the same slice.
/// </remarks>
public record SliceSyntax(
    SliceType Type,
    string Name,
    IEnumerable<EventSyntax> Events,
    IEnumerable<CommandSyntax> Commands,
    IEnumerable<QuerySyntax> Queries,
    IEnumerable<ProjectionSyntax> Projections,
    IEnumerable<CaptureSyntax> Captures,
    IEnumerable<ReactionSyntax> Reactions,
    IEnumerable<ScreenSyntax> Screens,
    IEnumerable<ConstraintSyntax> Constraints,
    IEnumerable<SpecificationSyntax> Specifications,
    SourceLocation Location,
    string? Description = null,
    IEnumerable<ReadModelSyntax>? ReadModels = null,
    IEnumerable<ReducerSyntax>? Reducers = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the <see cref="FileReferenceSyntax"/> naming the file the slice is realized by - the one
    /// file a slice's backend artifacts are kept in by convention - and <c>null</c> when the document
    /// does not name one.
    /// </summary>
    /// <remarks>
    /// An <c>init</c> property rather than a parameter of the primary constructor, deliberately. A trailing
    /// parameter on a positional record is source compatible and <em>binary</em> breaking: it replaces the
    /// constructor and <c>Deconstruct</c> in the compiled signature, so a package built against the previous
    /// version fails at run time with a missing method and no compiler error anywhere. Adding capability as
    /// an init property is neither, and is how this record should grow from here.
    /// </remarks>
    public FileReferenceSyntax? File { get; init; }
}
