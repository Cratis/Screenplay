// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_the_event_is_standalone : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(StandaloneSource);

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_keep_the_copy_in_generation_one() => Generation(1).Properties.Select(property => property.Name).ShouldContainOnly("projectId", "name");
    [Fact] void should_drop_the_copy_from_generation_two() => Generation(2).Properties.Select(property => property.Name).ShouldContainOnly("name");
    [Fact] void should_keep_the_contract_identity() => Result.Workspace!.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(Workspace.IdentityCatalog.EventContracts.Single().Id);
    [Fact] void should_keep_the_original_property_identity() => Property(Result.Workspace!, "projectId").SemanticId.ShouldEqual(Copy(Workspace).SemanticId);
    [Fact] void should_advance_the_contract_revision() => Result.Workspace!.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
    [Fact] void should_label_an_evolution_draft() => Result.PropertyReport!.EvolutionDraft.ShouldBeTrue();
    [Fact] void should_extract_nothing() => Result.PropertyReport!.ExtractedInlineEvents.ShouldBeEmpty();
    [Fact] void should_not_claim_deployability() => Result.PropertyReport!.DeployableWithoutTransformation.ShouldBeFalse();
    [Fact] void should_no_longer_copy_the_identifier() => StillCopiesTheIdentifier.ShouldBeFalse();
}
