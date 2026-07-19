// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>concept</c> declaration - a strongly typed domain value.
/// </summary>
/// <param name="Name">The name of the concept.</param>
/// <param name="Type">The primitive type of the concept, or <c>Enum</c> for enumeration concepts.</param>
/// <param name="Attributes">The attributes applied to the concept, without the <c>@</c> prefix, such as <c>pii</c> and <c>sensitive</c>.</param>
/// <param name="Values">The values of the concept when it is an enumeration, empty otherwise.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ConceptSyntax(
    string Name,
    string Type,
    IEnumerable<string> Attributes,
    IEnumerable<string> Values,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the well known primitive type names a concept can be based on.
    /// </summary>
    public static readonly IEnumerable<string> PrimitiveTypes = ["Uuid", "String", "Int", "Decimal", "Bool", "Date", "DateTime"];

    /// <summary>
    /// Gets a value indicating whether the concept is an enumeration.
    /// </summary>
    public bool IsEnum => Type == "Enum";
}
