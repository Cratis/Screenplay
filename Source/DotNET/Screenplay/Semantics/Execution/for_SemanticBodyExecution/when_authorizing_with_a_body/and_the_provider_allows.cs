// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_provider_allows : given.a_policy
{
    void Because() => Result = Execute();

    [Fact] void should_accept_the_command() => Result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_invoke_the_verified_policy_once() => ReceivedBodies.Single().Content.ShouldEqual(Body);
    [Fact] void should_append_the_declared_fact() => Result.World.Facts.Length.ShouldEqual(1);
}
