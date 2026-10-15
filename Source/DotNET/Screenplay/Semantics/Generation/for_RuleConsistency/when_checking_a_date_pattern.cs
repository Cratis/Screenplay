// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_checking_a_date_pattern : Specification
{
    SemanticConcept _concept;
    int _assessmentYear;
    ImmutableArray<RuleConsistencyResult> _results;
    RuleConsistencyResult _ruleResult;

    void Establish()
    {
        _assessmentYear = DateTimeOffset.UtcNow.Year;
        _concept = new(default, "DatedCode", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.Length, new SemanticNumberValue(8), null)])
        {
            Generation = new("pattern") { Pattern = "{yyyy}-#{4}" }
        };
    }

    void Because()
    {
        _results = RuleConsistency.Check(_concept);
        _ruleResult = RuleConsistency.Check(_concept.Generation, _concept.Primitive, _concept.Validations.Single());
    }

    [Fact] void should_agree_across_public_entry_points() => _results.Single().ShouldEqual(_ruleResult);
    [Fact] void should_refute_with_a_genuine_date_instead_of_leaving_the_rule_unproven() => _results.Single().Status.ShouldEqual(RuleConsistencyStatus.Refuted);
    [Fact] void should_use_the_current_utc_year_for_the_witness() => _results.Single().Witness.ShouldEqual($"{_assessmentYear.ToString("D4", CultureInfo.InvariantCulture)}-0000");
}
