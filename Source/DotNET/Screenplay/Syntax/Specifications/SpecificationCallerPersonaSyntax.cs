// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Names the persona used to synthesize a specification caller.
/// </summary>
/// <param name="Name">The top-level persona name.</param>
/// <param name="Location">The reference's source location.</param>
public record SpecificationCallerPersonaSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);
