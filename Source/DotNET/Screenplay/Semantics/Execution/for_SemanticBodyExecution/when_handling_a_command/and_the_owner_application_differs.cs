// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_owner_application_differs : given.a_command_handler
{
    void Establish()
    {
        var body = Bodies.Single(body => body.Requirement.Role == SemanticImplementationRole.CommandHandler);
        Bodies = Bodies.Replace(body, body with { Requirement = body.Requirement with { Owner = given.other_application.Of(body.Requirement.Owner) } });
    }
    void Because() => Result = Execute();
    [Fact] void should_refuse_the_body() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_call_the_provider() => Provider.HandleCalls.ShouldEqual(0);
}
