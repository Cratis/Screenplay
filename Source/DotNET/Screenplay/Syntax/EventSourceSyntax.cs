// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an application-owned event source classification, not an identity destination.
/// </summary>
/// <param name="Name">The authored name.</param>
/// <param name="Location">The declaration location.</param>
public record EventSourceSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets the optional identifier value type.</summary>
    public TypeRefSyntax? Identifier { get; init; }

    /// <summary>Gets the streams owned by this physical declaration.</summary>
    public IEnumerable<EventStreamSyntax> Streams { get; init; } = [];

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the old stored name retained only for a rename, not a semantic id.</summary>
    public string? Id { get; init; }
}

/// <summary>
/// Represents a stream owned by one event source declaration.
/// </summary>
/// <param name="Name">The authored name within its source.</param>
/// <param name="Location">The declaration location.</param>
public record EventStreamSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets the optional stream id value type; absence denotes an unkeyed stream.</summary>
    public TypeRefSyntax? StreamId { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the old stored name retained only for a rename, not a semantic id.</summary>
    public string? Id { get; init; }
}

/// <summary>
/// Represents an authored command route, separate from a production's identity destination.
/// </summary>
/// <param name="EventSource">The exact application-owned source name.</param>
/// <param name="Stream">The exact source-owned stream name.</param>
/// <param name="Location">The route header location.</param>
public record CommandStreamSyntax(string EventSource, string Stream, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets the optional authored stream id mapping.</summary>
    public PropertyMappingSyntax? StreamId { get; init; }

    /// <summary>Gets the parser-owned start of the qualified route reference.</summary>
    public SourceLocation ReferenceLocation { get; init; } = Location;

    /// <summary>Gets the exact UTF-16 reference length, or null without source evidence.</summary>
    [SourceSpanMetadata]
    public int? ReferenceLength { get; init; }

    /// <summary>
    /// Gets the preserved property interpretation when the same header has both viable meanings.
    /// This candidate does not select routing; the ambiguity is blocking.
    /// </summary>
    public PropertySyntax? PropertyCandidate { get; init; }
}
