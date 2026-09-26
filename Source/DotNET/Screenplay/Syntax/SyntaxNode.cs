// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the base of every node in the Screenplay syntax tree.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record SyntaxNode(SourceLocation Location)
{
    static readonly IReadOnlyDictionary<string, SourceLocation> _noDirectiveLocations = new Dictionary<string, SourceLocation>();

    /// <summary>
    /// Gets comments owned by this node. These are server-owned source metadata, not typed syntax.
    /// </summary>
    [SourceSpanMetadata]
    public ImmutableArray<SourceComment> SourceComments { get; init; } = [];

    /// <summary>
    /// Gets authored positions of scalar directive lines, keyed by their printer directive names.
    /// These positions are source metadata, not typed syntax values.
    /// </summary>
    [SourceSpanMetadata]
    public IReadOnlyDictionary<string, SourceLocation> DirectiveLocations { get; init; } = _noDirectiveLocations;

    /// <summary>
    /// Gets the final automap mode parsed from the source, before any typed edits. This is source metadata,
    /// not a typed syntax value.
    /// </summary>
    [SourceSpanMetadata]
    public AutoMapMode? ParsedAutoMapMode { get; init; }
}
