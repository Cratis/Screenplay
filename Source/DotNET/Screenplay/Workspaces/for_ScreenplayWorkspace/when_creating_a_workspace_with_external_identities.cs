// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_creating_a_workspace_with_external_identities : given.a_workspace_with_external_identities
{
    SemanticApplication Application => Workspace.Compilation.Value.Model.Application;
    SemanticSlice Slice => Application.Modules.Single().Features.Single().Slices.Single();

    [Fact] void should_compile_the_workspace() => Workspace.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_friendly_application_name() => Application.Name.ShouldEqual("Projects");
    [Fact] void should_keep_the_independent_application_identity() => Workspace.IdentityCatalog.Application.ShouldEqual(StableApplicationIdentity);
    [Fact] void should_preserve_the_external_application_semantic_identity() => Application.Id.ShouldEqual(ApplicationSemanticIdentity);
    [Fact] void should_preserve_the_external_command_semantic_identity() => Slice.Commands.Single().Id.ShouldEqual(CommandSemanticIdentity);
    [Fact] void should_preserve_the_external_event_semantic_identity() => Slice.Events.Single().Id.ShouldEqual(EventSemanticIdentity);
    [Fact] void should_preserve_the_external_event_contract_identity() => Slice.Events.Single().ContractId.ShouldEqual(EventContractIdentity);
    [Fact] void should_keep_external_assignments_persisted() => Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Id == ApplicationSemanticIdentity || assignment.Id == CommandSemanticIdentity || assignment.Id == EventSemanticIdentity).All(assignment => assignment.Origin == SemanticIdentityOrigin.Persisted).ShouldBeTrue();
    [Fact] void should_materialize_every_other_current_identity() => Workspace.IdentityCatalog.Semantics.Length.ShouldBeGreaterThan(3);
}
