// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_model_has_no_compilation_provenance : given.a_command_handler
{
    void Establish() => Plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(Plan.Model))).Plan!;
    void Because() => Result = Execute();
    [Fact] void should_refuse_the_body() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_call_the_provider() => Provider.HandleCalls.ShouldEqual(0);
}
