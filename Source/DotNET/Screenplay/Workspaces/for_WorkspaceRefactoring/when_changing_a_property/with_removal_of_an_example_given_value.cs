// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_an_example_given_value : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource("""
        module Projects
          feature Registration
            slice StateChange Facts
              event Audited
                name String
                other String
              example Expected : Audited
                name = "Ada"
                other = "kept"
              command Confirm
                id String identifier
                other String
                produces Confirmed
                  for id
                  other = other
              event Confirmed
                other String
              specification Confirmation
                given Expected
                when Confirm
                  id = "1"
                  other = "kept"
                then Confirmed
                  other = "kept"
        """);

    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove") with { EventNeverPersisted = true });

    [Fact] void should_accept_fixture_only_removal() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_not_mislabel_a_given_as_an_assertion() => Result.PropertyReport.Changed.Any(change => change.WeakensAssertion).ShouldBeFalse();
}
