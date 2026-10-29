// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_provider_denies : given.a_policy
{
    void Establish() => Answer = SemanticPolicyBodyResult.Deny();
    void Because() => Result = Execute();

    [Fact] void should_reject_as_unauthorized() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_preserve_the_world() => ReferenceEquals(Result.World, SemanticWorld.Empty).ShouldBeTrue();
    [Fact] void should_not_append_facts() => Result.World.Facts.ShouldBeEmpty();
}
