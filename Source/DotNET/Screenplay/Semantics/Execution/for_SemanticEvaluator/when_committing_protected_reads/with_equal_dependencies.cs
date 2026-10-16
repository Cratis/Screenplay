// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_equal_dependencies : given.a_protected_command
{
    SemanticDecisionDependencies _first;
    SemanticDecisionDependencies _second;
    SemanticDecisionDependencies _other;

    void Because()
    {
        _first = _evaluator.Decide(_plan, _world, Request()).Dependencies;
        _second = _evaluator.Decide(_plan, _world, Request()).Dependencies;
        _other = _evaluator.Decide(_plan, _world, Request(lookup: "other")).Dependencies;
    }

    [Fact] void should_equal_dependencies_decided_again() => _first.ShouldEqual(_second);
    [Fact] void should_hash_equal_dependencies_alike() => _first.GetHashCode().ShouldEqual(_second.GetHashCode());
    [Fact] void should_differ_for_another_key() => _first.ShouldNotEqual(_other);
    [Fact] void should_differ_from_empty() => _first.ShouldNotEqual(SemanticDecisionDependencies.Empty);

    [Fact]
    void should_compare_event_contracts_as_sets()
    {
        var entry = _first.Entries.Single();
        var reordered = new SemanticDecisionDependencies.Entry(SemanticValue.Text("read"), [.. entry.EventContracts.Reverse()]);
        reordered.ShouldEqual(entry);
        reordered.GetHashCode().ShouldEqual(entry.GetHashCode());
    }

    [Fact]
    void should_differ_when_the_event_contracts_differ() =>
        new SemanticDecisionDependencies.Entry(SemanticValue.Text("read"), []).ShouldNotEqual(_first.Entries.Single());
}
