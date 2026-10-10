// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Declares the system identity under which a reaction's returned commands run.
/// </summary>
/// <param name="Kind">The identity kind, currently only <c>system</c>.</param>
/// <param name="Roles">The exact roles held by the system identity.</param>
/// <param name="Location">The location of the <c>runs as</c> declaration.</param>
public record ReactionIdentitySyntax(string Kind, IEnumerable<string> Roles, SourceLocation Location) : SyntaxNode(Location);
