// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_incompatible_operands : given.rule_examples
{
    Exception[] _absent;
    Exception[] _emptyCollections;

    void Because()
    {
        _absent = [.. Examples.Where(example => example.Rule.Kind != SemanticValidationRuleKind.NotEmpty)
            .Select(example => Catch.Exception(() => SemanticValidationRules.Check(example.Rule with { Operand = SemanticValue.Composite([]) }, SemanticValue.Null)))];
        _emptyCollections = [.. new[] { SemanticValidationRuleKind.AllGreaterThan, SemanticValidationRuleKind.AllGreaterThanOrEqual }
            .Select(kind => Catch.Exception(() => SemanticValidationRules.Check(new SemanticValidationRule(default, kind, SemanticValue.Text("2"), null), SemanticValue.Array([]))))];
    }

    [Fact] void should_check_the_operand_even_for_absent_values() => _absent.ShouldEachConformTo(error => error is InvalidSemanticContract);
    [Fact] void should_check_the_operand_even_for_empty_collections() => _emptyCollections.ShouldEachConformTo(error => error is InvalidSemanticContract);
}
