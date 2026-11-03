// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the application's named time-zone context members.
/// </summary>
/// <param name="Members">The declared members.</param>
/// <param name="Location">The source location.</param>
public record ApplicationContextSyntax(IEnumerable<ContextMemberSyntax> Members, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional description of the application context.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents a named application context member of type <c>time zone</c>.
/// </summary>
/// <param name="Name">The member name.</param>
/// <param name="Location">The source location.</param>
public record ContextMemberSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);
