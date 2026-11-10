// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Reports structural authoring differences; this is not an execution or equivalence verdict.
/// </summary>
/// <param name="Matching">The declaration matching mode.</param>
/// <param name="Complete">Whether all sections are complete.</param>
/// <param name="HasSemanticChange">True for known structural change, false for complete absence of change, otherwise null.</param>
/// <param name="BeforeExecutable">Whether the baseline executable model is available.</param>
/// <param name="AfterExecutable">Whether the candidate executable model is available.</param>
/// <param name="BeforeDeclarations">The baseline assigned and unassigned declaration group count.</param>
/// <param name="AfterDeclarations">The candidate assigned and unassigned declaration group count.</param>
/// <param name="Declarations">Declaration presence, rename and move changes.</param>
/// <param name="Members">Authored member changes.</param>
/// <param name="Events">Event contract changes.</param>
/// <param name="Specifications">Specification changes.</param>
/// <param name="Dependants">Direct indexed dependants of changed declarations.</param>
/// <param name="Identities">Persisted identity changes; empty in Address mode.</param>
/// <param name="Sections">Section coverage and typed gaps.</param>
/// <param name="NotCompared">Stable statements of what the comparison does not establish.</param>
public sealed record ModelDifference(
    DeclarationMatching Matching,
    bool Complete,
    bool? HasSemanticChange,
    bool BeforeExecutable,
    bool AfterExecutable,
    int BeforeDeclarations,
    int AfterDeclarations,
    IReadOnlyList<DeclarationChange> Declarations,
    IReadOnlyList<MemberChange> Members,
    IReadOnlyList<EventContractChange> Events,
    IReadOnlyList<SpecificationChange> Specifications,
    IReadOnlyList<DirectDependant> Dependants,
    IReadOnlyList<IdentityChange> Identities,
    IReadOnlyList<ComparisonSection> Sections,
    IReadOnlyList<string> NotCompared)
{
    /// <summary>
    /// Gets numeric identity changes within matched enumeration concepts.
    /// </summary>
    public IReadOnlyList<EnumerationMemberChange> Enumerations { get; init; } = [];

    /// <summary>Gets the separate fail-closed agent exposure comparison; legacy sections do not include these rows.</summary>
    [JsonIgnore]
    public AgentExposureComparison AgentExposure { get; init; } = new(AgentExposureCoverage.Complete, []);

    /// <summary>
    /// Gets the changes grouped by the slice that owns them, with the slices that depend on each changed slice.
    /// </summary>
    public SliceChangeSummary Slices { get; init; } = SliceChangeSummary.Empty;
}
