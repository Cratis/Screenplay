// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_an_explicitly_suppressed_automap_writer : given.a_multi_source_property_workspace
{
    void Establish() => UseBindableSource(PotentialAutoMapSource.Replace("        from A, B", "        from A\n          name = \"fixed\"\n        from B", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("A", name: "old") with { NewName = "name", EventNeverPersisted = true });

    [Fact] void should_accept_without_introducing_an_automap_writer() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_keep_the_explicit_constant_mapping() => Candidate.Contains("name = \"fixed\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_evolve_only_the_selected_event() => Result.PropertyReport.Generations.Single().Event.Name.ShouldEqual("A");
}
