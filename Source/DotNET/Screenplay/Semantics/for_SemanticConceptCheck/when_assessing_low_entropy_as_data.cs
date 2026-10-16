// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_assessing_low_entropy_as_data : given.a_declaration
{
    SemanticConceptCheckResult _result;

    void Because() => _result = SemanticConceptCheck.Check(Concept("String", new("nanoid", SourceLocation.Start) { Size = 8, Alphabet = "ab" }), 2026);

    [Fact] void should_count_distinct_values_exactly() => _result.Support!.Count.ShouldEqual(new BigInteger(256));
    [Fact] void should_report_the_display_bits() => _result.Support!.Bits.ShouldEqual(8d);
    [Fact] void should_report_the_identifier_minimum_is_not_met() => _result.Support!.MeetsIdentifierMinimum.ShouldEqual(false);
    [Fact] void should_report_the_collision_estimate() => _result.Support!.ValuesAtOnePercentCollision.ShouldEqual(GenerationEntropy.ValuesAtOnePercentCollision(8));
    [Fact] void should_not_emit_property_scoped_entropy_diagnostics() => _result.Findings.ShouldBeEmpty();
    [Fact] void should_require_builtin_generation_admission() => _result.RequiredVersion.ShouldEqual(SemanticVersion.V15);
}
