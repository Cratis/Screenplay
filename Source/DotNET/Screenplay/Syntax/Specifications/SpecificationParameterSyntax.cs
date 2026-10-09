// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Declares a typed value supplied by every case of a specification table.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The required type reference.</param>
/// <param name="Location">The declaration location.</param>
public record SpecificationParameterSyntax(string Name, TypeRefSyntax Type, SourceLocation Location) : SyntaxNode(Location);
