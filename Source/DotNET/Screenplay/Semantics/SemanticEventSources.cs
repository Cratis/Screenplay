// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a declared event source and its stored identity.
/// </summary>
/// <param name="Id">The stable catalog identity.</param>
/// <param name="Name">The declaration name.</param>
/// <param name="SourceKind">The stored source name: its explicit pin, otherwise its name.</param>
/// <param name="Streams">The source's streams.</param>
public sealed record SemanticEventSource(SemanticId Id, string Name, string SourceKind, ImmutableArray<SemanticEventStream> Streams)
{
    /// <summary>
    /// Gets the source's declared identifier type, when present.
    /// </summary>
    public SemanticTypeReference? IdentifierType { get; init; }
}

/// <summary>
/// Represents an unkeyed, scalar-keyed or composite-keyed event stream.
/// </summary>
/// <param name="Id">The stable catalog identity.</param>
/// <param name="Name">The declaration name.</param>
/// <param name="StreamKind">The stored stream name: its explicit pin, otherwise its name.</param>
public sealed record SemanticEventStream(SemanticId Id, string Name, string StreamKind)
{
    /// <summary>
    /// Gets the scalar key type, mutually exclusive with composite parts.
    /// </summary>
    public SemanticTypeReference? StreamIdType { get; init; }

    /// <summary>
    /// Gets composite key parts in declaration order, or an empty collection.
    /// </summary>
    public ImmutableArray<SemanticStreamIdPart> StreamIdParts { get; init; } = [];
}

/// <summary>
/// Represents one declared part of a composite stream identity.
/// </summary>
/// <param name="Name">The exact part name.</param>
/// <param name="Type">The required portable scalar type.</param>
public sealed record SemanticStreamIdPart(string Name, SemanticTypeReference Type);
