// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_an_existing_root_gains_a_child : given.a_record_capture
{
    SemanticSpecificationRun _run;

    void Establish() => CompileScenario("""
              specification NewChild
                given capture Records
                  id = "root"
                  sourceStatus = "sendt"
                  items = [{"id":"old","value":"before"}]
                when capture Records
                  id = "root"
                  sourceStatus = "sendt"
                  items = [{"id":"old","value":"after"},{"id":"new","value":"after"}]
                then ChildChanged
                then ChildAnyChanged
                then ChildTransitioned
                then ChildAdded
        """);

    void Because() => _run = Run("NewChild");

    [Fact] void should_modify_the_known_child_but_only_add_the_new_child() => _run.Passed.ShouldBeTrue();
    [Fact] void should_append_exactly_four_facts() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(4);
}
