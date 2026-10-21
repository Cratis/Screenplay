// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>persona</c> declaration - a named role interacting with the application, with its associated policies.
/// </summary>
/// <param name="Name">The name of the persona.</param>
/// <param name="Description">The optional description of the persona.</param>
/// <param name="Policies">The names of the policies associated with the persona.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PersonaSyntax(
    string Name,
    string? Description,
    IEnumerable<string> Policies,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets what the persona is trying to achieve, one entry per <c>goal</c> line. Authoring metadata, never part of the executable model.
    /// </summary>
    public IEnumerable<string> Goals { get; init; } = [];

    /// <summary>
    /// Gets where the persona sits on the personas canvas (<c>position &lt;x&gt; &lt;y&gt;</c>), or <see langword="null"/> when no placement was authored.
    /// </summary>
    public int? X { get; init; }

    /// <summary>
    /// Gets where the persona sits on the personas canvas (<c>position &lt;x&gt; &lt;y&gt;</c>), or <see langword="null"/> when no placement was authored.
    /// </summary>
    public int? Y { get; init; }

    /// <summary>
    /// Gets the width of the persona on the personas canvas (<c>size &lt;width&gt; &lt;height&gt;</c>), or <see langword="null"/> when the canvas decides.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Gets the height of the persona on the personas canvas (<c>size &lt;width&gt; &lt;height&gt;</c>), or <see langword="null"/> when the canvas decides.
    /// </summary>
    public int? Height { get; init; }
}
