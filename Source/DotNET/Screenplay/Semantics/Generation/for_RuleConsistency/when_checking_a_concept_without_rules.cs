// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_checking_a_concept_without_rules : Specification
{
    ImmutableArray<RuleConsistencyResult> _results;

    void Because() => _results = RuleConsistency.Check(new(default, "Id", SemanticPrimitiveType.Uuid, [], []));

    [Fact] void should_produce_no_rule_results() => _results.ShouldBeEmpty();
    [Fact] void should_produce_an_initialized_array() => _results.IsDefault.ShouldBeFalse();
}
