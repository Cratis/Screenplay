// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a property declaration on an event or command.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="Type">The <see cref="TypeRefSyntax"/> of the property.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PropertySyntax(string Name, TypeRefSyntax Type, SourceLocation Location) : SyntaxNode(Location);
