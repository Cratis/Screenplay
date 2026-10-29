// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_a_body_is_not_resolved : given.a_rule
{
    void Because() => Result = new SemanticEvaluator(Provider, []).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_fail_closed() => ((SemanticUnsupported)Result).Details.ShouldContain("No resolved body is available.");
    [Fact] void should_not_call_the_provider() => Context.ShouldBeNull();
}
