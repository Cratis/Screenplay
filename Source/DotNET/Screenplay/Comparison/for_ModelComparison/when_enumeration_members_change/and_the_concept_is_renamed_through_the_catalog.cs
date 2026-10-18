// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison.for_ModelComparison.when_enumeration_members_change;

public class and_the_concept_is_renamed_through_the_catalog : given.an_enumeration
{
    SemanticId _conceptId;

    void Establish()
    {
        var concept = _before.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Concept && assignment.Address.Name == "Status");
        _conceptId = concept.Id;
        var catalog = SemanticIdentityCatalog.Create(
            _before.IdentityCatalog.Application,
            _before.IdentityCatalog.Documents,
            [.. _before.IdentityCatalog.Semantics.Select(assignment => assignment.Id == concept.Id ? assignment with { Address = SemanticAddress.ForConcept(_before.IdentityCatalog.Application, "InvoiceStatus"), Origin = SemanticIdentityOrigin.Persisted } : assignment)],
            _before.IdentityCatalog.EventContracts);
        ChangeSource(Model("paid\n  issued\n  draft").Replace("Status", "InvoiceStatus", StringComparison.Ordinal), catalog);
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_keep_identity_matching() => _result.Matching.ShouldEqual(DeclarationMatching.Identity);
    [Fact] void should_report_the_concept_rename() => _result.Declarations.Any(change => change.Declaration.SemanticId == _conceptId && change.Change == DeclarationChangeKind.Renamed).ShouldBeTrue();
    [Fact] void should_report_both_renumbered_members() => _result.Enumerations.Count.ShouldEqual(2);
    [Fact] void should_keep_the_concept_identity_in_enum_changes() => _result.Enumerations.All(change => change.Declaration.SemanticId == _conceptId && change.Declaration.BeforeAddress == "Status" && change.Declaration.AfterAddress == "InvoiceStatus").ShouldBeTrue();
    [Fact] void should_use_baseline_storage_usage() => _result.Enumerations.All(change => change.Stored).ShouldBeTrue();
}
