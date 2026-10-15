// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_previous_record_lacked_a_field : given.a_record_capture
{
    SemanticSpecificationRun _run;

    void Establish() => CompileScenario("""
              specification NewlyPresentField
                given capture Records
                  id = "root"
                  sourceStatus = "sendt"
                when capture Records
                  id = "root"
                  sourceStatus = "sendt"
                  label = "new"
                then AnyChanged
        """);

    void Because() => _run = Run("NewlyPresentField");

    [Fact] void should_still_treat_a_new_field_in_a_known_record_as_changed() => _run.Passed.ShouldBeTrue();
}
