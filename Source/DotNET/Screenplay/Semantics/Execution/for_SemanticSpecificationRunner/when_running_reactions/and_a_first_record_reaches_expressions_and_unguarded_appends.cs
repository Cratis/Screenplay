// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_first_record_reaches_expressions_and_unguarded_appends : given.a_v6_scenario
{
    void Establish() => Compile("""
        module Billing
          feature Import
            slice Translate Records
              capture Records
                key id
                map
                  status = sourceStatus translate
                    "betalt" => paid
                    "sendt" => sent
                append Flagged
                  when `status == "paid"`
                    status = $.status
                append Observed
                  status = $.status
              event Flagged
                status String
              event Observed
                status String
              specification FirstPaid
                when capture Records
                  id = "root"
                  sourceStatus = "betalt"
                then Flagged
                  status = "paid"
                then Observed
                  status = "paid"
              specification FirstSent
                when capture Records
                  id = "root"
                  sourceStatus = "sendt"
                then Observed
                  status = "sent"
        """);

    [Fact] void should_evaluate_an_expression_on_the_mapped_first_record() => Run("FirstPaid").Passed.ShouldBeTrue();
    [Fact] void should_still_append_without_a_guard_even_if_the_expression_does_not_hold() => Run("FirstSent").Passed.ShouldBeTrue();
}
