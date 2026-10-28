// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_body_content_has_changed : given.a_rule
{
    void Because() => Result = new SemanticEvaluator(Provider, [new(Requirement, "changed")]).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_fail_closed() => ((SemanticUnsupported)Result).Details.ShouldContain("Resolved body content does not match its requirement hash.");
    [Fact] void should_not_call_the_provider() => Provider.DidNotReceive().Evaluate(Arg.Any<SemanticImplementationBody>(), Arg.Any<SemanticRuleContext>());
}
