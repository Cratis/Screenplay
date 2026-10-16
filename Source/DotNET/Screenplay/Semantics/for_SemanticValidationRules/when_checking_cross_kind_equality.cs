// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_cross_kind_equality : Specification
{
    Exception[] _errors;

    void Because()
    {
        SemanticValue[] values = [SemanticValue.Text("2"), SemanticValue.Number(2), SemanticValue.Boolean(true), SemanticValue.Array([]), SemanticValue.Composite([])];
        _errors = [.. new[] { SemanticValidationRuleKind.Equal, SemanticValidationRuleKind.NotEqual }
            .SelectMany(kind => values.SelectMany(operand => values
                .Where(value => value.GetType() != operand.GetType() || value is SemanticArrayValue or SemanticCompositeValue)
                .Select(value => Catch.Exception(() => SemanticValidationRules.Check(new SemanticValidationRule(default, kind, operand, null), value)))))];
    }

    [Fact] void should_check_every_incompatible_pair() => _errors.Length.ShouldEqual(44);
    [Fact] void should_reject_incompatible_kinds_instead_of_comparing_them() => _errors.ShouldEachConformTo(error => error is InvalidSemanticContract);
}
