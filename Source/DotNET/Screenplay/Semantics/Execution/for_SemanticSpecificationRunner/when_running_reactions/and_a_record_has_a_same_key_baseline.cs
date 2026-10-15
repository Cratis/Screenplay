// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_record_has_a_same_key_baseline : given.a_record_capture
{
    SemanticSpecificationRun _run;

    void Establish() => CompileScenario("""
              specification Modification
                given capture Records
                  id = "root"
                  sourceStatus = "sendt"
                  label = "previous"
                when capture Records
                  id = "root"
                  sourceStatus = "betalt"
                  label = "current"
                then Changed
                then AnyChanged
                then AllChanged
                then Transitioned
        """);

    void Because() => _run = Run("Modification");

    [Fact] void should_compare_against_the_given_record_with_the_same_key() => _run.Passed.ShouldBeTrue();
    [Fact] void should_append_modifications_and_not_an_addition() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(4);
}
