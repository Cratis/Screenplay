// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_inspecting_a_reserved_strategy : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("sequence", SourceLocation.Start)), 2026);

    [Fact] void should_report_the_existing_reservation() => _result.Findings.Single().Code.ShouldEqual(DiagnosticCodes.ReservedSequenceGeneration);
    [Fact] void should_target_the_generation_declaration() => _result.Findings.Single().Target.ShouldEqual(SemanticConceptFindingTarget.Generation);
    [Fact] void should_not_report_support_or_proofs_for_a_reservation() => _result.Support.ShouldBeNull();
}
