// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_bodies_are_ambiguous : given.a_rule
{
    void Because() => Result = new SemanticEvaluator(Provider, [Bodies.Single(), Bodies.Single()]).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_fail_closed() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_call_the_provider() => Context.ShouldBeNull();
}
