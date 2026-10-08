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
/// Represents one named, scalar part of a composite stream id.
/// </summary>
/// <param name="Name">The exact part name.</param>
/// <param name="Type">The nonoptional scalar value type.</param>
/// <param name="Location">The part declaration location.</param>
public record EventStreamIdPartSyntax(string Name, TypeRefSyntax Type, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a stream owned by one event source declaration.
/// </summary>
/// <param name="Name">The authored name within its source.</param>
/// <param name="Location">The declaration location.</param>
public record EventStreamSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets the optional scalar stream id value type.</summary>
    public TypeRefSyntax? StreamId { get; init; }

    /// <summary>Gets the composite stream id parts in declaration order.</summary>
    public IEnumerable<EventStreamIdPartSyntax> StreamIdParts { get; init; } = [];

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

    /// <summary>Gets the named composite part mappings in authored order.</summary>
    public IEnumerable<PropertyMappingSyntax> StreamIdParts { get; init; } = [];

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
