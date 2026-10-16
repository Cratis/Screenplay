// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_checking_a_semantic_concept : Specification
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(new SemanticConcept(default, "Reference", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.Length, SemanticValue.Number(4), null)]) { Generation = new("nanoid") { Size = 4, Alphabet = "ab" } }, 2026);

    [Fact] void should_prove_a_matching_rule() => _result.Proofs.Single().Status.ShouldEqual(RuleConsistencyStatus.Proven);
    [Fact] void should_leave_proven_rules_without_findings() => _result.Findings.ShouldBeEmpty();
    [Fact] void should_retain_the_supplied_concept_identity() => _result.Concept!.Id.ShouldEqual(default);
    [Fact] void should_share_the_source_support_calculation() => _result.Support!.Bits.ShouldEqual(4d);
    [Fact] void should_select_the_same_generated_rule_version() => _result.RequiredVersion.ShouldEqual(SemanticVersion.V20);
}
