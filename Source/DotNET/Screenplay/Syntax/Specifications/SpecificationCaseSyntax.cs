// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Supplies one named, complete set of parameter values for a specification table.
/// </summary>
/// <param name="Name">The stable case name.</param>
/// <param name="Values">The concrete parameter assignments.</param>
/// <param name="Location">The case declaration location.</param>
public record SpecificationCaseSyntax(string Name, IEnumerable<PropertyMappingSyntax> Values, SourceLocation Location) : SyntaxNode(Location);
