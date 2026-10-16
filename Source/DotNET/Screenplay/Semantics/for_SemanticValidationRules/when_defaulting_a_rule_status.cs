// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_defaulting_a_rule_status : Specification
{
    SemanticRuleStatus _status;

    void Because() => _status = default;

    [Fact] void should_not_imply_that_a_rule_passed() => _status.ShouldEqual(SemanticRuleStatus.NotEvaluable);
}
