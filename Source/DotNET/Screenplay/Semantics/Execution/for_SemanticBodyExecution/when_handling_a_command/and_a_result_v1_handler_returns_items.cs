// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_a_result_v1_handler_returns_items : given.a_command_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([]);
    void Because() => Result = Execute();

    [Fact] void should_be_unsupported() => ((SemanticUnsupported)Result).Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
    [Fact] void should_commit_nothing() => Result.World.Facts.ShouldBeEmpty();
}
