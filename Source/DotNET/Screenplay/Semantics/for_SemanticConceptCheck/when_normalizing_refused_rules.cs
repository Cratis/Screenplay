// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_normalizing_refused_rules : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("nanoid", SourceLocation.Start) { Size = 4, Alphabet = "ab" }, Rule(ValidationRuleKind.NotEmpty) with { Property = "anotherValue" }, Rule(ValidationRuleKind.Matches, "^z+$")), 2026);

    [Fact] void should_keep_the_refused_rule_at_its_authored_index() => _result.Findings.Single(finding => finding.Code == DiagnosticCodes.UnsupportedSemanticSyntax).RuleIndex.ShouldEqual(0);
    [Fact] void should_not_shift_the_following_rules_finding() => _result.Findings.Single(finding => finding.Code == DiagnosticCodes.RefutedGeneratedConceptRule).RuleIndex.ShouldEqual(1);
    [Fact] void should_only_prove_bound_rules() => _result.Proofs.Length.ShouldEqual(1);
    [Fact] void should_keep_proofs_parallel_to_bound_rules() => _result.Concept!.Validations.Length.ShouldEqual(_result.Proofs.Length);
}
