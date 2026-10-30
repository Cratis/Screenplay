// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_admission_is_refused : given.a_reaction_effect
{
    [Theory]
    [InlineData("hash")]
    [InlineData("role")]
    [InlineData("member")]
    [InlineData("source")]
    [InlineData("provider")]
    [InlineData("capability")]
    void should_not_call_the_provider_for_an_unverified_body(string kind)
    {
        Bodies = [AlterBody(kind, Bodies.Single())];
        if (kind == "source") Bodies = [];
        AssertRefused(kind == "provider" ? new SemanticEvaluator() : null);
    }

    [Theory]
    [InlineData(2U, 1U)]
    [InlineData(1U, 2U)]
    void should_refuse_an_unsupported_version(uint context, uint result)
    {
        var body = Bodies.Single();
        Bodies = [body with { Requirement = body.Requirement with { ContextVersion = context, ResultVersion = result } }];
        AssertRefused();
    }

    [Theory]
    [InlineData("composition")]
    [InlineData("clock")]
    [InlineData("several")]
    void should_refuse_unsupported_triggers(string kind)
    {
        var trigger = Reaction.Triggers.Single();
        Reaction = Reaction with
        {
            Triggers = kind switch
            {
                "composition" => [trigger with { Produces = [new(Target, null, null, [])] }],
                "clock" => [trigger with { Kind = SemanticReactionTriggerKind.Interval }],
                _ => [trigger, trigger]
            }
        };
        AssertRefused();
    }

    static SemanticImplementationBody AlterBody(string kind, SemanticImplementationBody body) => kind switch
    {
        "hash" => body with { Content = "changed" },
        "role" => body with { Requirement = body.Requirement with { Role = SemanticImplementationRole.RulePredicate } },
        "member" => body with { Requirement = body.Requirement with { Member = "Other/when Requested" } },
        "capability" => body with { Requirement = body.Requirement with { RequiredCapability = "pure" } },
        _ => body
    };

    void AssertRefused(ISemanticEvaluator? evaluator = null)
    {
        Fire(evaluator);
        Failure.ShouldBeOfExactType<SemanticUnsupported>();
        Provider.Calls.ShouldEqual(0);
        Loop.Facts.ShouldBeEmpty();
    }
}
