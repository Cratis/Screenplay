// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_checking_with_an_explicit_year : Specification
{
    SemanticConcept _concept;
    ImmutableArray<RuleConsistencyResult> _results;

    void Establish() => _concept = new(default, "DatedCode", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.Length, new SemanticNumberValue(8), null)])
    {
        Generation = new("pattern") { Pattern = "{yyyy}-#{4}" }
    };

    void Because() => _results = RuleConsistency.Check(_concept, 1999);

    [Fact] void should_use_the_supplied_year_in_the_counterexample() => _results.Single().Witness.ShouldEqual("1999-0000");
    [Fact] void should_refute_the_length_rule() => _results.Single().Status.ShouldEqual(RuleConsistencyStatus.Refuted);
}
