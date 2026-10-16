// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_refusing_empty_conditioned_support : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("pattern", SourceLocation.Start) { Pattern = "000000006-[AB]", Checksum = "mod11" }), 2026);

    [Fact] void should_preserve_the_binder_diagnostic() => _result.Findings.Single().Code.ShouldEqual(DiagnosticCodes.EmptyGenerationSupport);
    [Fact] void should_target_the_generation_declaration() => _result.Findings.Single().Target.ShouldEqual(SemanticConceptFindingTarget.Generation);
    [Fact] void should_not_report_a_successful_support_assessment() => _result.Support.ShouldBeNull();
}
