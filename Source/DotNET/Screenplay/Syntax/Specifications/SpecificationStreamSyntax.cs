// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents the explicit route of one specification event occurrence.
/// </summary>
/// <param name="EventSource">The source declaration name.</param>
/// <param name="Stream">The stream declaration name.</param>
/// <param name="Location">The route directive location.</param>
public record SpecificationStreamSyntax(string EventSource, string Stream, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the concrete stream id mapping, when the stream is keyed.
    /// </summary>
    public PropertyMappingSyntax? StreamId { get; init; }

    /// <summary>
    /// Gets the named composite part mappings in authored order.
    /// </summary>
    public IEnumerable<PropertyMappingSyntax> StreamIdParts { get; init; } = [];

    /// <summary>
    /// Gets the source and stream reference location.
    /// </summary>
    public SourceLocation ReferenceLocation { get; init; } = Location;

    /// <summary>
    /// Gets the source and stream reference length.
    /// </summary>
    [SourceSpanMetadata]
    public int? ReferenceLength { get; init; }
}

/// <summary>
/// Represents an assertion that an expected event occurrence is unrouted.
/// </summary>
/// <param name="Location">The directive location.</param>
public record SpecificationNoStreamSyntax(SourceLocation Location) : SyntaxNode(Location);
