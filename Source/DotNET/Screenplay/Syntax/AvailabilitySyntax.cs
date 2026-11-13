// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an <c>audience</c> declaration - a named group a tenant, user or system caller can belong to at runtime.
/// </summary>
/// <param name="Name">The name of the audience.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A Screenplay audience is who a part of the application is available to. It is unrelated to a token's <c>aud</c> claim.
/// The model declares audiences and never who is in them.
/// </remarks>
public record AudienceSyntax(string Name, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the report-only description of the audience.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents an <c>availability</c> declaration - a named, reusable rule deciding whether a part of the model is there.
/// </summary>
/// <param name="Name">The name of the availability.</param>
/// <param name="Requirements">The requirements, one per <c>for</c>, <c>when</c> or <c>with</c> line; all of them must hold.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AvailabilitySyntax(string Name, IEnumerable<AvailabilityRequirementSyntax> Requirements, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the report-only description of the availability.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents an <c>available for | when | with</c> gate on a module, feature, slice, command, query or screen.
/// </summary>
/// <param name="Requirement">The <see cref="AvailabilityRequirementSyntax"/> that must hold for the part to be there.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Gates on the path from the module down to the construct all have to hold, and a gate is decided before <c>authorize</c>.
/// </remarks>
public record AvailableSyntax(AvailabilityRequirementSyntax Requirement, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets every audience the requirement names, in the order they appear.
    /// </summary>
    /// <returns>The <see cref="AudienceAvailabilityRequirementSyntax">audience references</see> held anywhere in the requirement.</returns>
    public IEnumerable<AudienceAvailabilityRequirementSyntax> Audiences() => AvailabilityRequirementSyntax.Leaves<AudienceAvailabilityRequirementSyntax>(Requirement);

    /// <summary>
    /// Gets every availability the requirement names, in the order they appear.
    /// </summary>
    /// <returns>The <see cref="AvailabilityReferenceSyntax">availability references</see> held anywhere in the requirement.</returns>
    public IEnumerable<AvailabilityReferenceSyntax> References() => AvailabilityRequirementSyntax.Leaves<AvailabilityReferenceSyntax>(Requirement);
}

/// <summary>
/// Represents the base of what an availability requires - audiences, a condition or another availability, combined.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record AvailabilityRequirementSyntax(SourceLocation Location) : SyntaxNode(Location)
{
    internal static IEnumerable<T> Leaves<T>(AvailabilityRequirementSyntax requirement)
        where T : AvailabilityRequirementSyntax => requirement switch
        {
            T leaf => [leaf],
            LogicalAvailabilityRequirementSyntax logical => Leaves<T>(logical.Left).Concat(Leaves<T>(logical.Right)),
            _ => []
        };
}

/// <summary>
/// Represents <c>for &lt;Audience&gt;</c> - holds when the caller's tenant, user or system holds the audience.
/// </summary>
/// <param name="Audience">The name of the audience.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AudienceAvailabilityRequirementSyntax(string Audience, SourceLocation Location) : AvailabilityRequirementSyntax(Location);

/// <summary>
/// Represents <c>with &lt;Availability&gt;</c> - holds when the named, reusable availability holds.
/// </summary>
/// <param name="Availability">The name of the availability.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AvailabilityReferenceSyntax(string Availability, SourceLocation Location) : AvailabilityRequirementSyntax(Location);

/// <summary>
/// Represents <c>when &lt;condition&gt;</c> - holds when the condition over <c>$context</c> is true.
/// </summary>
/// <param name="Condition">The <see cref="ConditionSyntax"/> that must hold.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ConditionAvailabilityRequirementSyntax(ConditionSyntax Condition, SourceLocation Location) : AvailabilityRequirementSyntax(Location);

/// <summary>
/// Represents two availability requirements combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand <see cref="AvailabilityRequirementSyntax"/>.</param>
/// <param name="Operator">The <see cref="LogicalOperator"/> combining them.</param>
/// <param name="Right">The right hand <see cref="AvailabilityRequirementSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LogicalAvailabilityRequirementSyntax(
    AvailabilityRequirementSyntax Left,
    LogicalOperator Operator,
    AvailabilityRequirementSyntax Right,
    SourceLocation Location) : AvailabilityRequirementSyntax(Location);
