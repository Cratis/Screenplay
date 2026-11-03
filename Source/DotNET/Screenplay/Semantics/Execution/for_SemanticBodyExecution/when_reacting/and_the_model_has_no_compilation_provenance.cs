// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_model_has_no_compilation_provenance : given.a_reaction_effect
{
    void Establish() => Plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(Plan.Model))).Plan!;
    void Because() => Fire();
    [Fact] void should_refuse_the_body() => Failure.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_call_the_provider() => Provider.Calls.ShouldEqual(0);
    [Fact] void should_not_append_facts() => Loop.Facts.ShouldBeEmpty();
}
