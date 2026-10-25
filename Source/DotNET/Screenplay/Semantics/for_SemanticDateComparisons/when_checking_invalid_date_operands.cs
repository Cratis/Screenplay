// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_invalid_date_operands : Specification
{
    Exception?[] _errors;

    void Because() => _errors = new[] { SemanticValue.Text("not a date"), SemanticValue.Number(42) }
        .Select(value => Catch.Exception(() => SemanticDateComparisons.Check(new(default, SemanticValidationRuleKind.GreaterThan, value, null), SemanticValue.Text("2026-01-01"), SemanticPrimitiveType.Date))).ToArray();

    [Fact] void should_refuse_malformed_and_incompatible_operands() => _errors.ShouldEachConformTo(error => error is InvalidSemanticContract);
}
