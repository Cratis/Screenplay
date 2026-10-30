// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_caller_is_missing : given.a_policy
{
    void Establish() => Request = Request with { Caller = null };
    void Because() => Result = Execute();

    [Fact] void should_deny() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_not_invoke_the_body() => Contexts.ShouldBeEmpty();
}
