// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents a syntax-only response expectation.
/// </summary>
/// <param name="Location">The assertion location.</param>
public abstract record SpecificationReturnSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an expected scalar response value.
/// </summary>
/// <param name="Value">The concrete expected value.</param>
/// <param name="Location">The assertion location.</param>
public record ScalarSpecificationReturnSyntax(ExpressionSyntax Value, SourceLocation Location) : SpecificationReturnSyntax(Location);

/// <summary>
/// Represents a nonempty subset of expected record response fields.
/// </summary>
/// <param name="Fields">The concrete field assertions in authored order.</param>
/// <param name="Location">The assertion location.</param>
public record RecordSpecificationReturnSyntax(IEnumerable<PropertyMappingSyntax> Fields, SourceLocation Location) : SpecificationReturnSyntax(Location);
