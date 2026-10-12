// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_explicit_projection_mapping : given.a_property_add_workspace
{
    void Because()
    {
        var from = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is FromSyntax);
        Result = Workspace.ProposeProperty(AddRequest() with { EventNeverPersisted = true, FixtureValue = "\"Normal\"", Hops = [AddHop("Registered"), MappingHop(), AddHop("Project", "importance"), new(from.Handle, Value: "priority")] });
    }

    [Fact] void should_accept_the_explicit_from_mapping() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_add_both_mapping_blocks() => Result.PropertyReport.Changed.Count(change => change.Role == "mapping-target").ShouldEqual(2);
    [Fact] void should_use_the_selected_read_model_name() => Candidate.ShouldContain("importance = priority");
}
