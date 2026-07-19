// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Defines how automatic mapping of same named properties behaves for a scope.
/// </summary>
public enum AutoMapMode
{
    /// <summary>
    /// Inherit the automap behavior from the enclosing scope; the default at the top is enabled.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Automatic mapping is disabled with <c>no automap</c>.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Automatic mapping is explicitly enabled with <c>automap</c>.
    /// </summary>
    Enabled = 2
}

/// <summary>
/// Represents a <c>projection</c> declaration - how events are folded into a read model.
/// </summary>
/// <param name="Name">The name of the projection.</param>
/// <param name="ReadModel">The name of the target read model, or <c>null</c> when it should be inferred.</param>
/// <param name="Sequence">The optional event sequence the projection reads from, declared with <c>sequence</c>.</param>
/// <param name="AutoMap">The <see cref="AutoMapMode"/> for the projection.</param>
/// <param name="Key">The optional projection level <see cref="KeySyntax"/>.</param>
/// <param name="Blocks">The <see cref="ProjectionBlockSyntax">blocks</see> making up the projection body.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ProjectionSyntax(
    string Name,
    string? ReadModel,
    string? Sequence,
    AutoMapMode AutoMap,
    KeySyntax? Key,
    IEnumerable<ProjectionBlockSyntax> Blocks,
    SourceLocation Location) : SyntaxNode(Location);
