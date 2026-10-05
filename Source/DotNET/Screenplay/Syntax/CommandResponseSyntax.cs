// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a syntax-only command response, allocated to ESM v8 by decision 0023.
/// </summary>
/// <param name="Location">The response declaration location.</param>
public abstract record CommandResponseSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a response consisting of one command property value.
/// </summary>
/// <param name="Source">The property supplying the response.</param>
/// <param name="Location">The response declaration location.</param>
public record ScalarCommandResponseSyntax(PropertyResponseSourceSyntax Source, SourceLocation Location) : CommandResponseSyntax(Location);

/// <summary>
/// Represents an unnamed record response, preserving authored field order even with one field.
/// </summary>
/// <param name="Fields">The response fields.</param>
/// <param name="Location">The response declaration location.</param>
public record RecordCommandResponseSyntax(IEnumerable<ResponseFieldSyntax> Fields, SourceLocation Location) : CommandResponseSyntax(Location);

/// <summary>
/// Represents a named response field with an optional explicit type annotation.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="Type">The explicit type, or null to infer the source property's type.</param>
/// <param name="Source">The command property supplying the value.</param>
/// <param name="Location">The field declaration location.</param>
public record ResponseFieldSyntax(string Name, TypeRefSyntax? Type, PropertyResponseSourceSyntax Source, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a direct command property reference, not an executable expression.
/// </summary>
/// <param name="Property">The referenced property name.</param>
/// <param name="Location">The source operand location.</param>
public record PropertyResponseSourceSyntax(string Property, SourceLocation Location) : SyntaxNode(Location);
