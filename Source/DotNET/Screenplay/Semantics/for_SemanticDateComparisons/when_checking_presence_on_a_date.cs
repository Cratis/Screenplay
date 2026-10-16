// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_presence_on_a_date : Specification
{
    SemanticRuleCheck _result;

    void Because() => _result = SemanticDateComparisons.Check(new(default, SemanticValidationRuleKind.NotEmpty, null, null), SemanticValue.Null, SemanticPrimitiveType.Date);

    [Fact] void should_reject_absence() => _result.Status.ShouldEqual(SemanticRuleStatus.Failed);
    [Fact] void should_use_the_value_rule_message() => _result.Message.ShouldEqual("A required value is empty.");
}
