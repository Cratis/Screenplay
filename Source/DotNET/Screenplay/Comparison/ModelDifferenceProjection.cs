// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

static class ModelDifferenceProjection
{
    internal static ModelDifference Project(StructuralDifference difference, DeclarationMatching matching, bool beforeExecutable, bool afterExecutable) => new(
        matching,
        difference.Complete,
        difference.HasSemanticChange,
        beforeExecutable,
        afterExecutable,
        difference.BeforeDeclarations,
        difference.AfterDeclarations,
        [.. difference.Changes.Where(change => change.Section == "declarations").Select(change => new DeclarationChange(Declaration(change, matching), DeclarationKind(change.ChangeKind), Move(change.MoveKind), change.BeforeOwner, change.AfterOwner, Documents(change.BeforeDocuments), Documents(change.AfterDocuments)))],
        [.. difference.Changes.Where(change => change.Section == "members").Select(change => new MemberChange(Declaration(change, matching), change.Member!, MemberKind(change.ChangeKind), change.BeforeHash, change.AfterHash, change.ContractBreaking == true))],
        [.. difference.Changes.Where(change => change.Section == "events").Select(change => new EventContractChange(Declaration(change, matching), EventKind(change.ChangeKind), change.Member, change.BeforeType, change.AfterType, change.ContractBreaking == true, change.GenerationCovered == true, change.BeforeGeneration, change.AfterGeneration))],
        [.. difference.Changes.Where(change => change.Section == "specifications").Select(change => new SpecificationChange(Declaration(change, matching), SpecificationKind(change.ChangeKind), change.Member, change.BeforeHash, change.AfterHash))],
        [.. difference.Changes.Where(change => change.Section == "dependants").Select(change => new DirectDependant(Declaration(ChangedDeclaration(change, difference), matching), change.Snapshot == "before" ? ComparedSide.Before : ComparedSide.After, change.DependantAddress!, change.Role!, Resolution(change.Resolution!)))],
        [.. difference.Changes.Where(change => change.Section == "identities").Select(change => new IdentityChange(Declaration(change, matching), IdentityKind(change.ChangeKind), change.EventContractId is { } id ? EventContractId.Parse(id) : null))],
        [.. difference.Sections.Select(section => new ComparisonSection(SectionKind(section.Section), section.Complete, [.. section.Gaps.Select(gap => new ComparisonGap(GapKind(gap.Kind), gap.Statement))]))],
        difference.Limits);

    static ComparedDeclaration Declaration(StructuralComparison.Change change, DeclarationMatching matching) => new(
        change.Kind,
        change.BeforeAddress,
        change.AfterAddress,
        matching == DeclarationMatching.Identity && change.SemanticId is { } id ? SemanticId.Parse(id) : null);

    static StructuralComparison.Change ChangedDeclaration(StructuralComparison.Change dependant, StructuralDifference difference) => difference.Changes.FirstOrDefault(change => change.Section != "dependants" &&
        (dependant.SemanticId is not null ? change.SemanticId == dependant.SemanticId : change.SemanticId is null && change.Kind == dependant.Kind &&
            ((dependant.BeforeAddress is not null && change.BeforeAddress == dependant.BeforeAddress) || (dependant.AfterAddress is not null && change.AfterAddress == dependant.AfterAddress)))) ?? dependant;

    static ModelDocumentLocation[] Documents(StructuralComparison.DocumentLocation[]? documents) => documents is null ? [] : [.. documents.Select(document => new ModelDocumentLocation(DocumentId.Parse(document.DocumentId), document.Path))];

    static DeclarationChangeKind DeclarationKind(string kind) => kind switch
    {
        "added" => DeclarationChangeKind.Added,
        "removed" => DeclarationChangeKind.Removed,
        "renamed" => DeclarationChangeKind.Renamed,
        "moved" => DeclarationChangeKind.Moved,
        _ => throw Unknown(kind)
    };

    static DeclarationMove? Move(string? kind) => kind switch
    {
        null => null,
        "owner" => DeclarationMove.Owner,
        "document" => DeclarationMove.Document,
        _ => throw Unknown(kind)
    };

    static MemberChangeKind MemberKind(string kind) => kind switch
    {
        "changed" => MemberChangeKind.Changed,
        "opaque-changed" => MemberChangeKind.OpaqueChanged,
        _ => throw Unknown(kind)
    };

    static EventContractChangeKind EventKind(string kind) => kind switch
    {
        "property-added" => EventContractChangeKind.PropertyAdded,
        "property-removed" => EventContractChangeKind.PropertyRemoved,
        "property-type-changed" => EventContractChangeKind.PropertyTypeChanged,
        "generation-added" => EventContractChangeKind.GenerationAdded,
        "generation-removed" => EventContractChangeKind.GenerationRemoved,
        _ => throw Unknown(kind)
    };

    static SpecificationChangeKind SpecificationKind(string kind) => kind switch
    {
        "added" => SpecificationChangeKind.Added,
        "removed" => SpecificationChangeKind.Removed,
        "expected-outcome-changed" => SpecificationChangeKind.ExpectedOutcomeChanged,
        "opaque-changed" => SpecificationChangeKind.OpaqueChanged,
        _ => throw Unknown(kind)
    };

    static IdentityChangeKind IdentityKind(string kind) => kind switch
    {
        "assigned" => IdentityChangeKind.Assigned,
        "retired" => IdentityChangeKind.Retired,
        "migrated" => IdentityChangeKind.Migrated,
        _ => throw Unknown(kind)
    };

    static DependantResolution Resolution(string kind) => kind switch
    {
        "resolved" => DependantResolution.Resolved,
        "unresolved" => DependantResolution.Unresolved,
        "ambiguous" => DependantResolution.Ambiguous,
        "incomplete" => DependantResolution.Incomplete,
        "wrongKind" => DependantResolution.WrongKind,
        _ => throw Unknown(kind)
    };

    static ComparisonSectionKind SectionKind(string kind) => kind switch
    {
        "declarations" => ComparisonSectionKind.Declarations,
        "events" => ComparisonSectionKind.Events,
        "members" => ComparisonSectionKind.Members,
        "specifications" => ComparisonSectionKind.Specifications,
        "dependants" => ComparisonSectionKind.Dependants,
        "identities" => ComparisonSectionKind.Identities,
        _ => throw Unknown(kind)
    };

    static ComparisonGapKind GapKind(StructuralGapKind kind) => kind switch
    {
        StructuralGapKind.IncompleteSource => ComparisonGapKind.IncompleteSource,
        StructuralGapKind.ExampleResolution => ComparisonGapKind.ExampleResolution,
        StructuralGapKind.NotComparableAssigned => ComparisonGapKind.NotComparableAssigned,
        StructuralGapKind.NotComparableIndexed => ComparisonGapKind.NotComparableIndexed,
        StructuralGapKind.AddressKeysOnly => ComparisonGapKind.AddressKeysOnly,
        StructuralGapKind.UnassignedEventContracts => ComparisonGapKind.UnassignedEventContracts,
        StructuralGapKind.UnassignedSpecifications => ComparisonGapKind.UnassignedSpecifications,
        StructuralGapKind.UnresolvedDependants => ComparisonGapKind.UnresolvedDependants,
        StructuralGapKind.IdentitiesNotCompared => ComparisonGapKind.IdentitiesNotCompared,
        StructuralGapKind.DeclarationLevelOnly => ComparisonGapKind.DeclarationLevelOnly,
        _ => throw Unknown(kind.ToString())
    };

    static InvalidSemanticContract Unknown(string kind) => new($"Unknown structural comparison value '{kind}'.");
}
