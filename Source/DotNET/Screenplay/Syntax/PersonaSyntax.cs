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
    SourceLocation Location) : SyntaxNode(Location);
