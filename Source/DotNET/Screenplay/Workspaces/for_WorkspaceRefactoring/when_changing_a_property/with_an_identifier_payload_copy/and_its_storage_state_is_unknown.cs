// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

// Without a release baseline nothing says whether the event was stored: the draft must neither
// assume it was never persisted nor claim the new generation can be deployed as is.
public class and_its_storage_state_is_unknown : given.an_identifier_payload_copy_workspace
{
    void Establish() => Create(InlineSource);

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_report_that_there_is_no_baseline() => Result.PropertyReport!.Generations.Single().Mutability.Basis.ShouldEqual("noBaseline");
    [Fact] void should_draft_a_new_generation_rather_than_edit_in_place() => Result.PropertyReport!.Generations.Single().Mode.ShouldEqual("newGeneration");
    [Fact] void should_label_an_evolution_draft() => Result.PropertyReport!.EvolutionDraft.ShouldBeTrue();
    [Fact] void should_not_claim_deployability() => Result.PropertyReport!.DeployableWithoutTransformation.ShouldBeFalse();
}
