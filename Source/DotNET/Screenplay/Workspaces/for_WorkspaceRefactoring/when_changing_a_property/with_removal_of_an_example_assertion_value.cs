// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_an_example_assertion_value : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(ExampleSource);

    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove") with { EventNeverPersisted = true });

    [Fact] void should_accept_the_disclosed_removal() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_disclose_the_inherited_assertion_at_its_consumer() => Result.PropertyReport.Changed.Any(change => change.Role == "assertion" && change.WeakensAssertion && change.Location.Line == 14).ShouldBeTrue();
    [Fact] void should_disclose_the_example_fixture_separately() => Result.PropertyReport.Changed.Any(change => change.Role == "fixture" && !change.WeakensAssertion && change.Location.Line == 8).ShouldBeTrue();
}
