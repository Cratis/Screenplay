// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>user</c> declaration - a named person a development or test session can act as, who plays zero or more personas.
/// </summary>
/// <remarks>
/// A user is authoring metadata for development and test tooling, like a persona; it is never part of the executable model.
/// The roles a user holds come from the policies of the personas it plays.
/// </remarks>
/// <param name="Name">The name of the user, which is also the identity a development session presents.</param>
/// <param name="DisplayName">The optional display name; the name stands in when it is absent.</param>
/// <param name="Personas">The names of the personas the user plays.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record UserSyntax(
    string Name,
    string? DisplayName,
    IEnumerable<string> Personas,
    SourceLocation Location) : SyntaxNode(Location);
