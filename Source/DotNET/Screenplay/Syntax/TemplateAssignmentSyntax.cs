// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a scoped <c>template &lt;Name&gt;</c> assignment of an own template, or a
/// <c>template blueprint &lt;Blueprint&gt; &lt;Name&gt; [version "&lt;range&gt;"]</c> assignment of a template a blueprint package ships.
/// </summary>
/// <param name="Name">The assigned template name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateAssignmentSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the name of the blueprint package the template comes from, or <c>null</c> for an own template.
    /// </summary>
    public string? Blueprint { get; init; }

    /// <summary>
    /// Gets the semver range of the blueprint the reference accepts, such as <c>^1.2</c>, or <c>null</c> when it does not pin a version.
    /// Only a blueprint reference carries one.
    /// </summary>
    public string? Version { get; init; }
}
