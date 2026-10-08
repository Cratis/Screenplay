// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents one named, possibly partial event, command or read-model fixture.
/// </summary>
/// <param name="Name">The name used in specification steps.</param>
/// <param name="Type">The underlying event, command or read-model name, bare or qualified.</param>
/// <param name="Values">The authored property values.</param>
/// <param name="Location">The source location of the declaration.</param>
public record SpecificationExampleSyntax(
    string Name,
    string Type,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional explanation of the fixture.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the explicit event-source or command destination fixture.
    /// </summary>
    public ExpressionSyntax? For { get; init; }

    /// <summary>
    /// Gets the optional event route supplied by this example.
    /// </summary>
    public SpecificationStreamSyntax? Stream { get; init; }

    /// <summary>
    /// Gets the explicit unrouted event assertion supplied by this example.
    /// </summary>
    public SpecificationNoStreamSyntax? NoStream { get; init; }

    /// <summary>
    /// Gets the generated command fixtures, separate from request values.
    /// </summary>
    public IEnumerable<PropertyMappingSyntax> GeneratedValues { get; init; } = [];
}
