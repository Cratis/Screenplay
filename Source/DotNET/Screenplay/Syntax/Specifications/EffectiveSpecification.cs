// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Identifies how an effective fixture value was supplied.
/// </summary>
public enum SpecificationValueOrigin
{
    /// <summary>
    /// The step states the value without an example.
    /// </summary>
    Authored,

    /// <summary>
    /// The example supplies the value.
    /// </summary>
    Example,

    /// <summary>
    /// The step replaces a value supplied by the example.
    /// </summary>
    Override,

    /// <summary>
    /// A persona policy supplies the caller atom.
    /// </summary>
    Persona,

    /// <summary>
    /// A named case supplies the parameter value.
    /// </summary>
    Case
}

/// <summary>
/// One effective fixture value and its source provenance.
/// </summary>
/// <param name="Property">The property, 'for', or 'generated' property name.</param>
/// <param name="Value">The effective expression, retaining its source location.</param>
/// <param name="Origin">How the value was supplied.</param>
/// <param name="OverriddenValue">The example expression replaced by the step, if any.</param>
public sealed record EffectiveSpecificationValue(string Property, ExpressionSyntax Value, SpecificationValueOrigin Origin, ExpressionSyntax? OverriddenValue)
{
    /// <summary>
    /// Gets the persona supplying a caller atom.
    /// </summary>
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the persona policy supplying a caller atom.
    /// </summary>
    public string? Policy { get; init; }

    /// <summary>
    /// Gets the parameter supplying this case value.
    /// </summary>
    public string? CaseParameter { get; init; }
}

/// <summary>
/// An expanded step alongside the unmodified authored step.
/// </summary>
/// <param name="Role">The step keyword, including its explicit kind.</param>
/// <param name="Authored">The original step.</param>
/// <param name="Effective">The expanded step.</param>
/// <param name="Example">The resolved example, if used.</param>
/// <param name="Values">Effective values and their provenance.</param>
public sealed record EffectiveSpecificationStep(string Role, SyntaxNode Authored, SyntaxNode Effective, SpecificationExampleSyntax? Example, IReadOnlyList<EffectiveSpecificationValue> Values)
{
    /// <summary>
    /// Gets the effective route and its source provenance, absent when no route was stated.
    /// </summary>
    public EffectiveSpecificationRoute? Route { get; init; }
}

/// <summary>
/// One effective route, replaced as a whole when overridden.
/// </summary>
/// <param name="Value">The effective stream or no-stream node.</param>
/// <param name="Origin">How the route was supplied.</param>
/// <param name="OverriddenValue">The example route replaced by the step, if any.</param>
public sealed record EffectiveSpecificationRoute(SyntaxNode Value, SpecificationValueOrigin Origin, SyntaxNode? OverriddenValue);

/// <summary>
/// A specification expanded without changing the authored syntax or matching rules.
/// </summary>
/// <param name="Authored">The original specification.</param>
/// <param name="Effective">The expanded specification.</param>
/// <param name="Steps">The supported fixture steps in scenario order.</param>
public sealed record EffectiveSpecification(SpecificationSyntax Authored, SpecificationSyntax Effective, IReadOnlyList<EffectiveSpecificationStep> Steps)
{
    /// <summary>
    /// Gets the named case expanded from the authored table.
    /// </summary>
    public SpecificationCaseSyntax? Case { get; init; }
}

/// <summary>
/// The effective application view and diagnostics from resolving its examples.
/// </summary>
/// <param name="Application">The application with expanded specifications.</param>
/// <param name="Specifications">The authored and effective specification pairs.</param>
/// <param name="Diagnostics">Resolution and example-body diagnostics; errors prevent binding.</param>
public sealed record EffectiveSpecificationApplication(ApplicationSyntax Application, IReadOnlyList<EffectiveSpecification> Specifications, IReadOnlyList<Diagnostic> Diagnostics)
{
    internal IReadOnlyList<ResolvedSpecificationExample> ResolvedExamples { get; init; } = [];
}

internal sealed record ResolvedSpecificationExample(SpecificationExampleSyntax Example, string Kind, SyntaxNode Type, IEnumerable<PropertySyntax> Properties);
