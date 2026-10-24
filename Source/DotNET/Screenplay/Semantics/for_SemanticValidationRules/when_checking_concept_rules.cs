// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_concept_rules : Specification
{
    SemanticConcept _concept;
    ImmutableArray<SemanticRuleCheck> _results;

    void Establish() => _concept = new(
        SemanticId.Create(SemanticKind.Concept, "Code"),
        "Code",
        SemanticPrimitiveType.Text,
        [],
        [
            new(default, SemanticValidationRuleKind.NotEmpty, null, null),
            new(default, SemanticValidationRuleKind.Length, SemanticValue.Number(3), "$strings.codeLength") { Severity = SemanticValidationSeverity.Warning },
            new(default, SemanticValidationRuleKind.Contains, SemanticValue.Text("A"), "Include A")
        ]);

    void Because() => _results = SemanticValidationRules.Check(_concept, SemanticValue.Null);

    [Fact] void should_return_every_rule_in_order() => _results.Select(check => check.Rule).ToArray().ShouldEqual(_concept.Validations.ToArray());
    [Fact] void should_keep_the_concept_identity() => _results.ShouldEachConformTo(check => check.Concept == _concept.Id);
    [Fact] void should_use_the_concept_presence_message() => _results[0].Message.ShouldEqual("A required concept value is empty.");
    [Fact] void should_preserve_string_keys_without_resolving_them() => _results[1].Message.ShouldEqual("$strings.codeLength");
    [Fact] void should_preserve_authored_messages_even_on_satisfied_rules() => _results[2].Message.ShouldEqual("Include A");
    [Fact] void should_keep_severity_on_the_original_rule() => _results[1].Rule.Severity.ShouldEqual(SemanticValidationSeverity.Warning);
}
