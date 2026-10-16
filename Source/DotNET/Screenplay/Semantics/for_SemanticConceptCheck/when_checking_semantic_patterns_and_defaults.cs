// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_checking_semantic_patterns_and_defaults : Specification
{
    SemanticConceptCheckResult _patterns;
    SemanticConceptCheckResult _generated;
    SemanticConceptCheckResult _ordinary;
    SemanticConceptCheckResult _invalidGeneration;

    void Because()
    {
        _patterns = SemanticConceptCheck.Check(new SemanticConcept(default, "Text", SemanticPrimitiveType.Text, [], [new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("["), null)]), 2026);
        var concept = new SemanticConcept(default, "Id", SemanticPrimitiveType.Uuid, [], [new(default, SemanticValidationRuleKind.CodeValidation, null, null)]);
        _ordinary = SemanticConceptCheck.Check(concept, 2026);
        _generated = SemanticConceptCheck.Check(concept, 2026, assumeGenerated: true);
        _invalidGeneration = SemanticConceptCheck.Check(new SemanticConcept(default, "Text", SemanticPrimitiveType.Text, [], []) { Generation = new("nanoid") { Size = 0 } }, 2026);
    }

    [Fact] void should_attribute_semantic_pattern_errors_to_the_validation_index() => _patterns.Findings.Single().RuleIndex.ShouldEqual(0);
    [Fact] void should_return_semantic_pattern_admission_details() => _patterns.Findings.Single().Pattern!.Status.ShouldEqual(PortableMatchStatus.Invalid);
    [Fact] void should_use_the_existing_pattern_diagnostic() => _patterns.Findings.Single().Code.ShouldEqual(DiagnosticCodes.InvalidMatchPattern);
    [Fact] void should_not_invent_default_proofs_without_generation_usage() => _ordinary.Proofs.ShouldBeEmpty();
    [Fact] void should_assess_assumed_default_rules_as_opaque() => _generated.Proofs.Single().Status.ShouldEqual(RuleConsistencyStatus.Unproven);
    [Fact] void should_require_v20_for_assumed_default_generation_with_rules() => _generated.RequiredVersion.ShouldEqual(SemanticVersion.V20);
    [Fact] void should_keep_generation_contract_errors_as_findings() => _invalidGeneration.Findings.Single().Target.ShouldEqual(SemanticConceptFindingTarget.Generation);
    [Fact] void should_not_count_invalid_semantic_support() => _invalidGeneration.Support.ShouldBeNull();
}
