// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_the_event_is_inline : given.an_identifier_payload_copy_workspace
{
    int _transactions;

    void Establish()
    {
        Create(InlineSource);
        _transactions = WorkspaceRepairVerification.TransactionCount(Workspace);
    }

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept_one_proposal() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_run_a_single_transaction() => WorkspaceRepairVerification.TransactionCount(Workspace).ShouldEqual(_transactions + 1);
    [Fact] void should_disclose_the_extracted_event() => Result.PropertyReport!.ExtractedInlineEvents.Single().Name.ShouldEqual("Renamed");
    [Fact] void should_label_an_evolution_draft() => Result.PropertyReport!.EvolutionDraft.ShouldBeTrue();
    [Fact] void should_not_claim_deployability() => Result.PropertyReport!.DeployableWithoutTransformation.ShouldBeFalse();
    [Fact] void should_keep_the_original_payload() => Generation(1).Properties.Select(property => property.Name).ShouldContainOnly("projectId", "name");
    [Fact] void should_drop_the_copy_from_the_new_generation() => Generation(2).Properties.Select(property => property.Name).ShouldContainOnly("name");
    [Fact] void should_route_explicitly_to_the_identifier() => Result.Workspace!.Documents.Single().Text.Contains("for projectId", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_drop_the_copy_mapping() => Production.Mappings.Select(mapping => mapping.Property).ShouldContainOnly("name");
    [Fact] void should_keep_the_contract_identity() => Result.Workspace!.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(Workspace.IdentityCatalog.EventContracts.Single().Id);
    [Fact] void should_keep_the_original_property_identity() => Property(Result.Workspace!, "projectId").SemanticId.ShouldEqual(Copy(Workspace).SemanticId);
    [Fact] void should_advance_the_contract_revision() => Result.Workspace!.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
    [Fact] void should_no_longer_copy_the_identifier() => StillCopiesTheIdentifier.ShouldBeFalse();
    [Fact] void should_retain_one_write_plan() => Result.WritePlan.ShouldNotBeNull();

    ProducesSyntax Production => WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(entry => entry.Node).OfType<ProducesSyntax>().Single();
}
