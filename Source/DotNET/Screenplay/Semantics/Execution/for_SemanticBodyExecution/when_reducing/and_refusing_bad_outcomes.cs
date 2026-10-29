// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reducing;

public class and_refusing_bad_outcomes : given.a_reducer
{
    [Theory]
    [InlineData("default")]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("missing-property")]
    [InlineData("default-properties")]
    [InlineData("null")]
    [InlineData("scalar")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    [InlineData("type")]
    [InlineData("key")]
    [InlineData("diagnostic")]
    [InlineData("delete-state")]
    void should_refuse_without_changing_the_input_world(string outcome)
    {
        Reset();
        var property = ReadModel.Properties.Single().Id;
        UseAnswer(outcome switch
        {
            "default" => new(default),
            "unknown" => new((SemanticReducerBodyOutcome)99),
            "missing" => new(SemanticReducerBodyOutcome.State),
            "missing-property" => SemanticReducerBodyResult.Next(SemanticValue.Composite([])),
            "default-properties" => SemanticReducerBodyResult.Next(new SemanticCompositeValue(default)),
            "null" => SemanticReducerBodyResult.Next(SemanticValue.Null),
            "scalar" => SemanticReducerBodyResult.Next(Key),
            "duplicate" => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(property, Key), new(property, Key)])),
            "extra" => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(property, Key), new(Plan.Commands.Values.Single().Id, Key)])),
            "type" => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(property, SemanticValue.Number(42))])),
            "key" => SemanticReducerBodyResult.Next(SemanticValue.Composite([new(property, SemanticValue.Text("other"))])),
            "diagnostic" => new(SemanticReducerBodyOutcome.State, State, "cannot compute\u001b[31m"),
            _ => new(SemanticReducerBodyOutcome.Delete, State)
        });
        var world = SemanticWorld.Empty;
        var result = Evaluator.Execute(Plan, world, Request());
        result.ShouldBeOfExactType<SemanticUnsupported>();
        ReferenceEquals(result.World, world).ShouldBeTrue();
        ((SemanticUnsupported)result).Details.ShouldContain("requirement");
        ((SemanticUnsupported)result).Details.Contains('\u001b').ShouldBeFalse();
        world.Facts.ShouldBeEmpty();
        world.ReadModels.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_a_throwing_or_cancelled_provider(bool cancel)
    {
        Reset();
        using var source = new CancellationTokenSource();
        Provider.Failure = cancel ? new OperationCanceledException() : new InvalidOperationException("secret-value");
        var result = (SemanticUnsupported)Evaluator.ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request(), source.Token);
        result.Details.ShouldContain("requirement");
        result.Details.ShouldNotContain("secret-value");
    }

    [Fact]
    void should_forward_cancellation_and_refuse_a_late_answer()
    {
        Reset();
        using var source = new CancellationTokenSource();
        Provider.Before = token =>
        {
            token.ShouldEqual(source.Token);
            source.Cancel();
        };
        Evaluator.ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request(), source.Token).ShouldBeOfExactType<SemanticUnsupported>();
    }

    [Fact]
    void should_refuse_a_null_provider_answer()
    {
        Reset();
        UseProvider(new(_ => null!));
        Evaluator.Execute(Plan, SemanticWorld.Empty, Request()).ShouldBeOfExactType<SemanticUnsupported>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_missing_or_stale_bodies(bool stale)
    {
        Reset();
        var bodies = stale ? Bodies.Select(body => body with { Content = "stale" }) : [];
        new SemanticEvaluator(Provider, bodies).Execute(Plan, SemanticWorld.Empty, Request()).ShouldBeOfExactType<SemanticUnsupported>();
        Contexts.ShouldBeEmpty();
    }

    [Fact]
    void should_preserve_the_no_provider_refusal()
    {
        Reset();
        var result = (SemanticUnsupported)new SemanticEvaluator().EstablishWorld(Plan, [Fact()]);
        result.Details.ShouldEqual("Reducer 'BalanceReducer' has opaque transitions and requires a target provider to compute read-model state.");
        Contexts.ShouldBeEmpty();
    }
}
