// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_RegisterProject_new_generation : given.a_RegisterProject_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(CorpusRequest() with { NewGeneration = true });

    [Fact] void should_bind_the_candidate() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_pass_every_model_specification() => Runs().SelectMany(run => run.Failures).ShouldBeEmpty();
    [Fact] void should_clone_the_current_shape_and_add_priority() => Events.Single(@event => @event.Generation == 3).Properties.Select(property => property.Name).ShouldContainOnly("name", "priority");
    [Fact] void should_preserve_generation_two() => Events.Single(@event => @event.Generation == 2).Properties.Select(property => property.Name).ShouldContainOnly("name");
    [Fact] void should_advance_the_catalog() => Result.Workspace.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(3u);
    [Fact] void should_disclose_revision_two_to_three() => Result.PropertyReport.Generations.Single().To.ShouldEqual(3u);
    [Fact] void should_remain_on_v4() => Result.Workspace.Compilation.Value.Model.SemanticVersion.ToString().ShouldEqual("4.0");
    [Fact] void should_have_no_esm_version_change() => Result.PropertyReport.EsmVersionChange.ShouldBeNull();
    [Fact] void should_not_claim_deployability() => Result.PropertyReport.DeployableWithoutTransformation.ShouldBeFalse();
}
