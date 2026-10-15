// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_first_record_has_child_and_nested_scopes : given.a_record_capture
{
    SemanticSpecificationRun _run;

    void Establish() => CompileScenario("""
              specification FirstObservation
                when capture Records
                  id = "root"
                  sourceStatus = "sendt"
                  label = "new"
                  items = [{"id":"child","value":"after"}]
                  contact = {"value":"after","label":"new"}
                then Added
                  for "root"
                  status = "sent"
                then ChildAdded
                  for "root"
                then NestedAdded
                  for "root"
        """);

    void Because() => _run = Run("FirstObservation");

    [Fact] void should_only_append_added_events_at_every_level() => _run.Passed.ShouldBeTrue();
    [Fact] void should_append_exactly_three_facts() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(3);
}
