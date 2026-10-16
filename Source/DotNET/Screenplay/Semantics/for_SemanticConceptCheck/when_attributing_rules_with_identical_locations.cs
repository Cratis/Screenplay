// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_attributing_rules_with_identical_locations : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", null, Rule(ValidationRuleKind.NotEmpty), Rule(ValidationRuleKind.Matches, "["), Rule(ValidationRuleKind.Matches, "(?<=x)y")), 2026);

    [Fact] void should_attribute_the_invalid_pattern_to_the_second_rule() => _result.Findings.Single(finding => finding.Code == DiagnosticCodes.InvalidMatchPattern).RuleIndex.ShouldEqual(1);
    [Fact] void should_attribute_the_refused_pattern_to_the_third_rule() => _result.Findings.Single(finding => finding.Code == DiagnosticCodes.PatternOutsidePortableMatchDialect).RuleIndex.ShouldEqual(2);
    [Fact] void should_identify_the_construct_not_a_source_line() => _result.Findings.ShouldEachConformTo(finding => finding.Target == SemanticConceptFindingTarget.Rule);
    [Fact] void should_return_the_pattern_check() => _result.Findings.Single(finding => finding.RuleIndex == 2).Pattern!.Construct.ShouldEqual("lookaround");
    [Fact] void should_not_invent_generation_for_ordinary_text() => _result.Support.ShouldBeNull();
}
