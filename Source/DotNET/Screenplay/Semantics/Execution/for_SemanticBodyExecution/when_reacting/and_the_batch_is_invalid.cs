// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_the_batch_is_invalid : given.a_reaction_effect
{
    [Theory]
    [InlineData("unknown")]
    [InlineData("malformed")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("default")]
    [InlineData("mixed")]
    [InlineData("unsupported")]
    void should_refuse_the_whole_batch(string kind)
    {
        Answer = InvalidAnswer(kind);
        Fire();
        Failure.ShouldBeOfExactType<SemanticUnsupported>();
        ((SemanticUnsupported)Failure!).Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
        ((SemanticUnsupported)Failure!).Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
        ((SemanticUnsupported)Failure!).Details.Any(char.IsControl).ShouldBeFalse();
        Loop.Facts.ShouldBeEmpty();
        Loop.Invocations.ShouldBeEmpty();
    }

    SemanticReactionBodyResult InvalidAnswer(string kind) => kind switch
    {
        "unknown" => SemanticReactionBodyResult.Produced([new(Target, Payload), new(default, Payload)]),
        "malformed" => SemanticReactionBodyResult.Produced([new(Target, Payload), new(Target, SemanticValue.Text("secret"))]),
        "missing" => SemanticReactionBodyResult.Produced([new(Target, SemanticValue.Composite([]))]),
        _ => InvalidShape(kind)
    };

    SemanticReactionBodyResult InvalidShape(string kind) => kind switch
    {
        "null" => null!,
        "default" => new(),
        "mixed" => new() { Events = [new(Target, Payload)], Commands = [new(Plan.Commands.Values.Single(command => command.Name == "Record").Id, Payload)] },
        _ => SemanticReactionBodyResult.Unsupported("diagnostic\nbody")
    };
}
