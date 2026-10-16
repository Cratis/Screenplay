// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_null_equality_operands : Specification
{
    SemanticRuleCheck[] _equal;
    SemanticRuleCheck[] _notEqual;

    void Because()
    {
        SemanticValue[] values = [SemanticValue.Text("a"), SemanticValue.Number(2), SemanticValue.Boolean(true)];
        _equal = [.. values.Select(value => SemanticValidationRules.Check(new SemanticValidationRule(default, SemanticValidationRuleKind.Equal, SemanticValue.Null, null), value))];
        _notEqual = [.. values.Select(value => SemanticValidationRules.Check(new SemanticValidationRule(default, SemanticValidationRuleKind.NotEqual, SemanticValue.Null, null), value))];
    }

    [Fact] void should_fail_equality_for_present_scalars() => _equal.ShouldEachConformTo(check => check.Status == SemanticRuleStatus.Failed);
    [Fact] void should_satisfy_inequality_for_present_scalars() => _notEqual.ShouldEachConformTo(check => check.Status == SemanticRuleStatus.Satisfied);
}
