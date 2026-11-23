// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_comparing_agent_exposure;

public class when_identity_survives_a_rename : given.agent_models
{
    void Establish()
    {
        _before = Create(Source + Agents(Query));
        _after = Create((Source + Agents(Query)).Replace("All", "Current", StringComparison.Ordinal), CatalogFor("List", "Current"));
        Assert.True(_after.Compilation.Success, string.Join("; ", _after.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_retain_allowlist_membership() => _result.AgentExposure.HasExposureChange.ShouldEqual(false);
    [Fact] void should_use_authoritative_identity() => _result.AgentExposure.Matching.ShouldEqual(DeclarationMatching.Identity);
    [Fact] void should_keep_structural_rename_reporting() => _result.Declarations.Any(change => change.Change == DeclarationChangeKind.Renamed).ShouldBeTrue();

    [Fact]
    void should_retain_membership_across_an_owner_move()
    {
        var moved = Source.Replace("      query All => Projects optional", "      query Other => Projects optional", StringComparison.Ordinal)
            + "\n      query All => Archive optional\n        by projectId Uuid" + Agents(Query.Replace("List.All", "Obsolete.All", StringComparison.Ordinal));
        var candidate = Create(moved, CatalogFor("Obsolete", "All"));
        Assert.True(candidate.Compilation.Success, string.Join("; ", candidate.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var exposure = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(candidate)).AgentExposure;
        exposure.Complete.ShouldBeTrue();
        exposure.HasExposureChange.ShouldEqual(false);
    }

    [Fact]
    void should_not_infer_continuity_without_authoritative_catalogs()
    {
        var exposure = ModelComparison.Compare(ComparedModel.WithoutIdentities(_before), ComparedModel.WithoutIdentities(_after)).AgentExposure;
        exposure.Changes.Select(change => change.Change).ShouldContainOnly(AgentExposureChangeKind.Added, AgentExposureChangeKind.Removed);
        exposure.Changes.All(change => (change.Before ?? change.After)!.Id is null).ShouldBeTrue();
    }

    SemanticIdentityCatalog CatalogFor(string slice, string name)
    {
        var original = SemanticAddress.ForQuery(SemanticAddress.ForSlice(_before.IdentityCatalog.Application, "Projects", "Registration", "List"), "All");
        var target = SemanticAddress.ForQuery(SemanticAddress.ForSlice(_before.IdentityCatalog.Application, "Projects", "Registration", slice), name);
        return SemanticIdentityCatalog.Create(_before.IdentityCatalog.Application, _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Select(assignment => assignment with { Address = Relocate(assignment.Address), Origin = SemanticIdentityOrigin.Persisted })],
            _before.IdentityCatalog.EventContracts);

        SemanticAddress Relocate(SemanticAddress address) => address.Equals(original) ? target
            : address.Kind == SemanticKind.QueryArgument && address.Equals(SemanticAddress.ForQueryArgument(original, address.Name)) ? SemanticAddress.ForQueryArgument(target, address.Name)
            : address;
    }
}
