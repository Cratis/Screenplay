// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_assessing_a_fixed_utc_year : given.a_declaration
{
    SemanticConceptCheckResult _first;
    SemanticConceptCheckResult _second;

    void Because()
    {
        var concept = Concept("String", new("pattern", SourceLocation.Start) { Pattern = "{yyyy}-[AB]" }, Rule(ValidationRuleKind.Length, 7d));
        _first = SemanticConceptCheck.Check(concept, 2024);
        _second = SemanticConceptCheck.Check(concept, 2025);
    }

    [Fact] void should_use_the_first_year_in_genuine_witnesses() => _first.Proofs.Single().Witness!.ShouldContain("2024");
    [Fact] void should_use_the_second_year_in_genuine_witnesses() => _second.Proofs.Single().Witness!.ShouldContain("2025");
    [Fact] void should_not_count_date_literals_as_entropy() => _first.Support!.Count.ShouldEqual(new BigInteger(2));
    [Fact] void should_not_mutate_the_declaration_between_assessments() => _second.Support!.Count.ShouldEqual(_first.Support!.Count);
}
