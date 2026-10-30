// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_the_result_is_not_evaluable : given.a_policy
{
    [Theory]
    [InlineData("default")]
    [InlineData("unknown")]
    [InlineData("null")]
    [InlineData("allow-diagnostic")]
    [InlineData("deny-diagnostic")]
    [InlineData("unsupported")]
    void should_refuse_without_claiming_a_model_denial(string answer)
    {
        Provider.Failure = null;
        Provider.Before = null;
        Answer = answer switch
        {
            "unknown" => new((SemanticPolicyBodyOutcome)99),
            "null" => null!,
            "allow-diagnostic" => new(SemanticPolicyBodyOutcome.Allow, "secret\n" + new string('x', 600)),
            "deny-diagnostic" => new(SemanticPolicyBodyOutcome.Deny, ""),
            "unsupported" => SemanticPolicyBodyResult.Unsupported("not available"),
            _ => new(default)
        };
        var result = (SemanticUnsupported)Execute();
        result.Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
        result.Details.ShouldContain("Permitted");
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
        result.Details.Contains('\n').ShouldBeFalse();
        result.Details.Length.ShouldBeLessThan(800);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_throwing_or_cancelled_execution(bool cancel)
    {
        Provider.Before = null;
        Provider.Failure = cancel ? new OperationCanceledException("private") : new InvalidOperationException("private");
        var result = (SemanticUnsupported)Execute();
        result.Details.ShouldContain(Provider.Failure.GetType().Name);
        result.Details.ShouldNotContain("private");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_cancellation_before_or_during_execution(bool before)
    {
        Provider.Failure = null;
        using var source = new CancellationTokenSource();
        if (before) source.Cancel();
        Provider.Before = token =>
        {
            token.ShouldEqual(source.Token);
            source.Cancel();
        };
        Execute(source.Token).ShouldBeOfExactType<SemanticUnsupported>();
    }
}
