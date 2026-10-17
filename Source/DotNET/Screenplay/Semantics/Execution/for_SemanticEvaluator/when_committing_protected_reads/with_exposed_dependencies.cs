// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_exposed_dependencies : given.a_protected_command
{
    SemanticDecisionDependencies _dependencies;

    void Because() => _dependencies = _evaluator.Decide(_plan, _world, Request()).Dependencies;

    [Fact] void should_expose_the_read_key() => _dependencies.Entries.Single().Key.ShouldEqual(SemanticValue.Text("read"));

    [Fact]
    void should_expose_the_from_and_removal_event_contracts_of_the_view() =>
        _dependencies.Entries.Single().EventContracts.ShouldContainOnly(
            _plan.Events.Values.Where(value => value.Name == "Recorded" || value.Name == "Removed").Select(value => value.Id));

    [Fact] void should_match_a_later_fact_at_the_key_with_a_handled_event() => _dependencies.Matches(Fact("Removed", "read")).ShouldBeTrue();
    [Fact] void should_not_match_the_same_event_at_another_key() => _dependencies.Matches(Fact("Removed", "write")).ShouldBeFalse();
    [Fact] void should_not_match_another_event_at_the_key() => _dependencies.Matches(Fact("OtherRecorded", "read", "x")).ShouldBeFalse();
    [Fact] void should_match_nothing_when_empty() => SemanticDecisionDependencies.Empty.Matches(Fact("Removed", "read")).ShouldBeFalse();
}
