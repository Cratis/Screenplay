// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Declares an authoring-only dependency on a module or feature.
/// </summary>
/// <param name="Target">The unqualified or dotted container name.</param>
/// <param name="Location">The location of the declaration.</param>
public record DependsOnSyntax(string Target, SourceLocation Location) : SyntaxNode(Location);
