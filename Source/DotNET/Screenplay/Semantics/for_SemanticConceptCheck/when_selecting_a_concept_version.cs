// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticConceptCheck;

public class when_selecting_a_concept_version : given.a_declaration
{
    [Fact] void should_keep_ordinary_rules_at_the_initial_version() => SemanticConceptCheck.Check(Concept("String", null, Rule(ValidationRuleKind.NotEmpty)), 2026).RequiredVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_select_phase_three_for_checksum_validation_alone() => SemanticConceptCheck.Check(Concept("String", null, Rule(ValidationRuleKind.Checksum, "luhn")), 2026).RequiredVersion.ShouldEqual(SemanticVersion.V21);
    [Fact] void should_select_the_text_rule_version() => SemanticConceptCheck.Check(Concept("String", null, Rule(ValidationRuleKind.StartsWith, "INV-")), 2026).RequiredVersion.ShouldEqual(SemanticVersion.V24);
    [Fact] void should_keep_an_explicit_unmodified_uuid_at_the_initial_version() => SemanticConceptCheck.Check(Concept("Uuid", new("uuid", SourceLocation.Start)), 2026).RequiredVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_select_phase_three_for_random_generation() => SemanticConceptCheck.Check(Concept("Int", new("random", SourceLocation.Start) { Range = "-9223372036854775808..9223372036854775807" }), 2026).Support!.Bits.ShouldEqual(64d);
}
