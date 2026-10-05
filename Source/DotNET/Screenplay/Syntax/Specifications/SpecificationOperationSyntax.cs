// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents an explicit operation failure fixture.
/// </summary>
/// <param name="Operation">The operation reference.</param>
/// <param name="Location">The fixture location.</param>
public record SpecificationOperationFailureSyntax(string Operation, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an assertion of a requested operation and its input values.
/// </summary>
/// <param name="Operation">The operation reference.</param>
/// <param name="Values">The partially asserted input values.</param>
/// <param name="Location">The assertion location.</param>
public record SpecificationOperationSyntax(string Operation, IEnumerable<PropertyMappingSyntax> Values, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an assertion that a declared compensation is requested.
/// </summary>
/// <param name="Operation">The operation reference.</param>
/// <param name="Location">The assertion location.</param>
public record SpecificationCompensatedSyntax(string Operation, SourceLocation Location) : SyntaxNode(Location);
