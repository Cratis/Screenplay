// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_an_overridden_example_value : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(ExampleSource.Replace("then Expected", "then Expected\n          name = \"Grace\"", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove") with { EventNeverPersisted = true });

    [Fact] void should_accept_the_removal() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_disclose_only_the_authored_override_assertion() => Result.PropertyReport.Changed.Where(change => change.WeakensAssertion).Select(change => change.Location.Line).ShouldContainOnly(15);
    [Fact] void should_not_count_the_overridden_example_again() => Result.PropertyReport.Changed.Any(change => change.WeakensAssertion && change.Location.Line == 14).ShouldBeFalse();
}
