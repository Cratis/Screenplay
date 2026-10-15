// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_record_has_a_different_key_baseline : given.a_record_capture
{
    SemanticSpecificationRun _run;

    void Establish() => CompileScenario("""
              specification FirstObservation
                given capture Records
                  id = 1
                  sourceStatus = "sendt"
                  label = "previous"
                when capture Records
                  id = "1"
                  sourceStatus = "betalt"
                  label = "current"
                then Added
                  for "1"
                  status = "paid"
        """);

    void Because() => _run = Run("FirstObservation");

    [Fact] void should_not_use_another_typed_keys_state_as_a_baseline() => _run.Passed.ShouldBeTrue();
    [Fact] void should_append_only_an_addition() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(1);
}
