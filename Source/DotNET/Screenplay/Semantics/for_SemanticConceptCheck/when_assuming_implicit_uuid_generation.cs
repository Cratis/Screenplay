// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_assuming_implicit_uuid_generation : given.a_declaration
{
    SemanticConceptCheckResult _ordinary;
    SemanticConceptCheckResult _generated;
    SemanticConceptCheckResult _text;

    void Because()
    {
        _ordinary = SemanticConceptCheck.Check(Concept("Uuid"), 2026);
        _generated = SemanticConceptCheck.Check(Concept("Uuid"), 2026, assumeGenerated: true);
        _text = SemanticConceptCheck.Check(Concept("String", null, Rule(ValidationRuleKind.NotEmpty)), 2026, assumeGenerated: true);
    }

    [Fact] void should_not_assess_an_unused_implicit_default() => _ordinary.Support.ShouldBeNull();
    [Fact] void should_assess_the_assumed_uuid_default() => _generated.Support!.Count.ShouldEqual(BigInteger.One << 122);
    [Fact] void should_meet_the_identifier_minimum() => _generated.Support!.MeetsIdentifierMinimum.ShouldEqual(true);
    [Fact] void should_not_invent_uuid_generation_for_text() => _text.Proofs.ShouldBeEmpty();
    [Fact] void should_not_select_generation_version_for_a_default_without_rules() => _generated.RequiredVersion.ShouldEqual(SemanticVersion.V1);
}
