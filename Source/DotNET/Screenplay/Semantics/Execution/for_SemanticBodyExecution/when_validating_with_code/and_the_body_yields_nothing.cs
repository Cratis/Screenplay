// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_body_yields_nothing : given.a_code_validation
{
    void Because() => Result = Execute();

    [Fact] void should_accept_the_command() => Result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_produce_the_event() => ((SemanticAccepted)Result).Facts.Length.ShouldEqual(1);
    [Fact] void should_call_once_per_block() => Contexts.Count.ShouldEqual(1);
}
