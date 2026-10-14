// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_generated_concept_rules : given.a_semantic_binder
{
    [Theory]
    [InlineData("matches \"^[A-Za-z0-9_-]{21}$\"")]
    [InlineData("not empty")]
    [InlineData("length == 21")]
    [InlineData("min 21")]
    [InlineData("max 21")]
    void should_admit_proven_rules_silently(string rule)
    {
        var result = Bind("concept InviteCode : String\n  generate nanoid\n  validate\n    " + rule);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V20);
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.RefutedGeneratedConceptRule || diagnostic.Code == DiagnosticCodes.UnprovenGeneratedConceptRule);
    }

    [Theory]
    [InlineData("length == 20")]
    [InlineData("max 20")]
    [InlineData("matches \"^INV-.*$\"")]
    [InlineData("length == 20 severity warning")]
    void should_refuse_a_rule_with_its_strategy_and_quoted_witness(string rule)
    {
        var result = Bind("concept InviteCode : String\n  generate nanoid\n  validate\n    " + rule);
        result.Success.ShouldBeFalse();
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.RefutedGeneratedConceptRule);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Location.Line.ShouldEqual(4);
        diagnostic.Message.ShouldContain("'nanoid'");
        diagnostic.Message.ShouldContain("\"---------------------\"");
    }

    [Fact]
    void should_warn_with_a_reason_for_a_passing_outside_fragment_rule()
    {
        var result = Bind("concept InviteCode : String\n  generate nanoid\n  validate\n    matches \"^(?=.{21}$).*$\"");
        result.Success.ShouldBeTrue();
        result.Diagnostics.Single(value => value.Code == DiagnosticCodes.UnprovenGeneratedConceptRule).Message.ShouldContain("lookaround");
    }

    [Fact]
    void should_not_activate_v20_for_an_unused_implicit_uuid_concept_with_rules() =>
        Bind("concept Id : Uuid\n  validate\n    rule Check\n      file Rules/Check.cs").Value!.Model.SemanticVersion.ShouldNotEqual(SemanticVersion.V20);
}
