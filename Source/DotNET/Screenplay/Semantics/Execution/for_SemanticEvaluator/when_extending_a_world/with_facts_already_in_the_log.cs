// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_extending_a_world;

public class with_facts_already_in_the_log : for_SemanticEvaluator.when_committing_protected_reads.given.a_protected_command
{
    const string Publisher = """
            slice Translate Outbound
              direction outbound
              public event Published
                value String
              projection Publisher => Published
                from Saved
                  value = value
        """;

    SemanticExecutionResult _result;

    void Establish() => Compile(Source + "\n" + Publisher);

    void Because() => _result = _evaluator.ExtendWorld(
        _plan,
        World(),
        [Fact("Saved", "first", "claimed"), Fact("Saved", "second", "claimed")]);

    [Fact] void should_not_check_constraints_against_facts_that_are_already_logged() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_publish_anything() => _result.World.Facts.Length.ShouldEqual(2);
    [Fact] void should_report_exactly_the_supplied_facts() => ((SemanticAccepted)_result).Facts.Length.ShouldEqual(2);
}
