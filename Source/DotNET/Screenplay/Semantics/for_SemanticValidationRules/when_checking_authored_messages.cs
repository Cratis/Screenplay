// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_authored_messages : given.rule_examples
{
    SemanticRuleCheck[] _results;

    void Because() => _results = [.. Examples.Select(example => SemanticValidationRules.Check(example.Rule with { Message = "An authored message" }, example.Failed))];

    [Fact] void should_keep_authored_text_instead_of_default_messages() => _results.ShouldEachConformTo(check => check.Message == "An authored message");
    [Fact] void should_not_change_the_failure_verdict() => _results.ShouldEachConformTo(check => check.Status == SemanticRuleStatus.Failed);
}
