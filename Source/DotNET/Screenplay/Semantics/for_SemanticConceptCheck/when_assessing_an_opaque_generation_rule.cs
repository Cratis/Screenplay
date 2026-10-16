// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_assessing_an_opaque_generation_rule : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("rule", SourceLocation.Start) { Rule = "MakeReference", Code = new("csharp", "return System.Guid.NewGuid().ToString();", SourceLocation.Start) }, Rule(ValidationRuleKind.NotEmpty)), 2026);

    [Fact] void should_bind_the_implementation_kind_without_a_source_document() => _result.Concept!.Generation!.Kind.ShouldEqual("implementation");
    [Fact] void should_keep_opaque_generation_unproven() => _result.Proofs.Single().Status.ShouldEqual(RuleConsistencyStatus.Unproven);
    [Fact] void should_report_the_existing_warning() => _result.Findings.Single().Code.ShouldEqual(DiagnosticCodes.UnprovenGeneratedConceptRule);
    [Fact] void should_attribute_the_warning_to_the_rule() => _result.Findings.Single().RuleIndex.ShouldEqual(0);
    [Fact] void should_not_fabricate_zero_support() => _result.Support!.Count.ShouldBeNull();
    [Fact] void should_not_treat_unknown_entropy_as_meeting_the_minimum() => _result.Support!.MeetsIdentifierMinimum.ShouldBeNull();
    [Fact] void should_require_phase_three() => _result.RequiredVersion.ShouldEqual(SemanticVersion.V21);
}
