// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_requirement_is_ambiguous : given.a_reaction_effect
{
    void Establish()
    {
        var body = Bodies.Single();
        Bodies = [body with { Requirement = body.Requirement with { IsAmbiguous = true } }];
    }
    void Because() => Fire();
    [Fact] void should_refuse_the_body() => Failure.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_call_the_provider() => Provider.Calls.ShouldEqual(0);
    [Fact] void should_not_append_facts() => Loop.Facts.ShouldBeEmpty();
}
