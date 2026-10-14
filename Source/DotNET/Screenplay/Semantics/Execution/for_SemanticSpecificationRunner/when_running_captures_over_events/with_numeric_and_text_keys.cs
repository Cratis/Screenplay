// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_numeric_and_text_keys : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _run;

    void Because()
    {
        Compile("""
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Numbered from "store"
                    id Int
                  event Named from "store"
                    id String
                  event Added
                  capture Feed
                    source events
                      from Numbered
                      from Named
                    key id
                    append Added
                      when added
                  specification S
                    given Numbered
                      id = 1
                    when append Named
                      id = "1"
                    then Added for "1"
            """);
        _run = Run("S");
    }

    [Fact] void should_keep_text_and_numeric_keys_separate() => _run.Passed.ShouldBeTrue();
}
