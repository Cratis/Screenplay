// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_checking_a_studio_built_declaration : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("nanoid", SourceLocation.Start) { Size = 0, Alphabet = "aa", Case = "upper" }), 2026);

    [Fact] void should_check_size_without_parsing_source_text() => _result.Findings.ShouldContain(finding => finding.Code == DiagnosticCodes.GenerationSizeOutsideRange && finding.Target == SemanticConceptFindingTarget.Option && finding.Option == "size");
    [Fact] void should_check_alphabet_without_parsing_source_text() => _result.Findings.ShouldContain(finding => finding.Code == DiagnosticCodes.InvalidGenerationAlphabet && finding.Option == "alphabet");
    [Fact] void should_check_modifier_applicability() => _result.Findings.ShouldContain(finding => finding.Code == DiagnosticCodes.InvalidGenerationModifier && finding.Option == "case");
    [Fact] void should_not_report_support_for_invalid_generation() => _result.Support.ShouldBeNull();
    [Fact] void should_not_prove_rules_against_invalid_generation() => _result.Proofs.ShouldBeEmpty();
}
