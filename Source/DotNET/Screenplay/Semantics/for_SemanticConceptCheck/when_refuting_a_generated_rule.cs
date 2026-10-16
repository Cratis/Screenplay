// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_refuting_a_generated_rule : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("nanoid", SourceLocation.Start) { Size = 4, Alphabet = "ab" }, Rule(ValidationRuleKind.Length, 5d) with { Severity = ValidationSeverity.Warning }), 2026);

    [Fact] void should_not_report_generation_failures() => _result.Findings.Where(finding => finding.Target == SemanticConceptFindingTarget.Generation).ShouldBeEmpty();
    [Fact] void should_return_the_refutation() => _result.Proofs.Single().Status.ShouldEqual(RuleConsistencyStatus.Refuted);
    [Fact] void should_return_a_genuine_witness() => _result.Proofs.Single().Witness.ShouldEqual("aaaa");
    [Fact] void should_keep_refutation_an_error_despite_rule_severity() => _result.Findings.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_use_the_existing_diagnostic() => _result.Findings.Single().Code.ShouldEqual(DiagnosticCodes.RefutedGeneratedConceptRule);
    [Fact] void should_target_the_rule() => _result.Findings.Single().RuleIndex.ShouldEqual(0);
    [Fact] void should_require_the_generated_rule_version() => _result.RequiredVersion.ShouldEqual(SemanticVersion.V20);
    [Fact] void should_keep_support_available_despite_refutation() => _result.Support!.Bits.ShouldEqual(4d);
}
