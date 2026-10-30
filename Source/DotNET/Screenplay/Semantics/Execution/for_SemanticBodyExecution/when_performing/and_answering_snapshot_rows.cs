// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_performing;

public class and_answering_snapshot_rows : given.a_reaction_effect
{
    SemanticPerformerContext _context;
    SemanticPerformerBodyResult _result;
    snapshot_provider _provider;
    ImmutableArray<SemanticCompositeValue> _rows;

    void Establish()
    {
        _rows = [Payload, new([new(Payload.Properties.Single().TargetProperty, SemanticValue.Text("Second"))])];
        _context = new(Payload, "tenant", new(true, ["Reader"], []), Occurred, _rows);
        _provider = new(SemanticPerformerBodyResult.Answered(_rows));
    }
    async Task Because() => _result = await _provider.PerformAsync(Bodies.Single(), _context, TestContext.Current.CancellationToken);

    [Fact] void should_preserve_row_order() => _result.Rows.ShouldEqual(_rows);
    [Fact] void should_pass_the_immutable_snapshot() => _provider.Context.ReadModels.ShouldEqual(_rows);
    [Fact] void should_pass_the_typed_arguments() => _provider.Context.Arguments.ShouldEqual(Payload);
    [Fact] void should_pass_the_tenant() => _provider.Context.Tenant.ShouldEqual("tenant");
    [Fact] void should_pass_the_caller_identity() => _provider.Context.Identity.ShouldEqual(_context.Identity);
    [Fact] void should_pass_received_at() => _provider.Context.ReceivedAt.ShouldEqual(Occurred);
    [Fact] void should_allow_an_empty_answer() => SemanticPerformerBodyResult.Answered([]).Rows.IsEmpty.ShouldBeTrue();
    [Fact] void should_distinguish_an_empty_answer_from_missing_rows() => SemanticPerformerBodyResult.Answered([]).Rows.IsDefault.ShouldBeFalse();
    [Fact] void should_keep_missing_rows_explicit() => new SemanticPerformerBodyResult().Rows.IsDefault.ShouldBeTrue();
    [Fact] void should_not_turn_unsupported_into_empty_success() => SemanticPerformerBodyResult.Unsupported("unavailable").Rows.IsDefault.ShouldBeTrue();

    sealed class snapshot_provider(SemanticPerformerBodyResult answer) : ISemanticBodyExecutionProvider
    {
        internal SemanticPerformerContext Context = null!;
        public ValueTask<SemanticPerformerBodyResult> PerformAsync(SemanticImplementationBody body, SemanticPerformerContext context, CancellationToken cancellationToken)
        {
            Context = context;
            return new(answer);
        }
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
