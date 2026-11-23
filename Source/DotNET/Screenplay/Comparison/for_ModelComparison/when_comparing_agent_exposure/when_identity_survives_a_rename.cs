// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_comparing_agent_exposure;

public class when_identity_survives_a_rename : given.agent_models
{
    static readonly string Model = Source.Replace("      query All => Projects optional\n        by projectId Uuid", "      query All => Projects optional", StringComparison.Ordinal);

    void Establish()
    {
        _before = Create(Model + Agents(Query));
        var catalog = SemanticIdentityCatalog.Create(_before.IdentityCatalog.Application, _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Select(assignment => assignment.Address.Kind == SemanticKind.Query && assignment.Address.Name == "All"
                ? assignment with { Address = SemanticAddress.ForQuery(SemanticAddress.ForSlice(_before.IdentityCatalog.Application, "Projects", "Registration", "List"), "Current"), Origin = SemanticIdentityOrigin.Persisted } : assignment)],
            _before.IdentityCatalog.EventContracts);
        _after = Create((Model + Agents(Query)).Replace("All", "Current", StringComparison.Ordinal), catalog);
        Assert.True(_after.Compilation.Success, string.Join("; ", _after.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_retain_allowlist_membership() => _result.AgentExposure.HasExposureChange.ShouldEqual(false);
    [Fact] void should_use_authoritative_identity() => _result.AgentExposure.Matching.ShouldEqual(DeclarationMatching.Identity);
    [Fact] void should_keep_structural_rename_reporting() => _result.Declarations.Any(change => change.Change == DeclarationChangeKind.Renamed).ShouldBeTrue();

    [Fact]
    void should_retain_membership_across_an_owner_move()
    {
        var catalog = SemanticIdentityCatalog.Create(_before.IdentityCatalog.Application, _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Select(assignment => assignment.Address.Kind == SemanticKind.Query && assignment.Address.Name == "All"
                ? assignment with { Address = SemanticAddress.ForQuery(SemanticAddress.ForSlice(_before.IdentityCatalog.Application, "Projects", "Registration", "Obsolete"), "All"), Origin = SemanticIdentityOrigin.Persisted } : assignment)],
            _before.IdentityCatalog.EventContracts);
        var moved = Model.Replace("      query All => Projects optional\n", string.Empty, StringComparison.Ordinal)
            + "\n      query All => Projects optional" + Agents(Query.Replace("List.All", "Obsolete.All", StringComparison.Ordinal));
        var candidate = Create(moved, catalog);
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
}
