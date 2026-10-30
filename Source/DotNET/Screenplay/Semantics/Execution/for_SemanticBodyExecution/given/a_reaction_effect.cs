// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_reaction_effect : Specification
{
    protected const string Body = "return effects;";
    protected const string Source = """
        module Orders
          feature Ordering
            slice Automation FollowUp
              event Requested
                name String
              event Recorded
                name String
              command Request
                name String
                produces Requested
                  for name
                  name = name
              command Record
                name String
                produces Recorded
                  for name
                  name = name
              reaction OnRequested
                runs as system role "Recorder"
                when Requested
                  name
                  ```csharp
                  return effects;
                  ```
              specification Followed
                given clock "2030-01-02T03:04:05Z"
                when append Requested
                  for "stream"
                  name = "First"
                then Recorded
                  for "stream"
                  name = "Handled"
        """;

    protected SemanticExecutionPlan Plan = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;
    protected effect_provider Provider = null!;
    internal SemanticReactionLoop Loop = null!;
    protected SemanticFact Cause = null!;
    protected SemanticReaction Reaction = null!;
    protected SemanticExecutionResult? Failure;
    protected SemanticReactionBodyResult Answer = null!;
    protected SemanticReactionContext Context = null!;
    protected SemanticCompositeValue Payload = null!;
    protected SemanticId Target;
    protected readonly DateTimeOffset Occurred = new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    void Establish()
    {
        Configure(Source);
        Provider = new((_, context) =>
        {
            Context = context;
            return Answer;
        });
    }

    protected void Configure(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("effects"), "effects", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        var planned = SemanticExecutionPlan.Compile(compilation.Value.Model);
        planned.Issues.ShouldBeEmpty();
        Plan = planned.Plan;
        Bodies = [.. compilation.ImplementationRequirements.Select(requirement => new SemanticImplementationBody(requirement, Body))];
        Reaction = Plan.Reactions.Single();
        var requested = Plan.Events.Values.Single(contract => contract.Name == "Requested");
        Cause = new(requested.Id, SemanticValue.Text("stream"), [new(requested.Properties.Single().Id, SemanticValue.Text("First"))])
        {
            Occurred = Occurred,
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("stream")))
        };
        var recorded = Plan.Events.Values.Single(contract => contract.Name == "Recorded");
        Target = recorded.Id;
        Payload = new([new(recorded.Properties.Single().Id, SemanticValue.Text("Handled"))]);
        Answer = SemanticReactionBodyResult.Produced([new(Target, Payload)]);
    }

    internal void Fire(ISemanticEvaluator? evaluator = null)
    {
        Loop = new(evaluator ?? new SemanticEvaluator(Provider, Bodies), Plan, SemanticWorld.Empty);
        Failure = Loop.Fire(Reaction, Reaction.Triggers[0], Cause.Values.ToDictionary(value => value.TargetProperty, value => value.Value), SemanticExpressionRootKind.Event, Cause, Occurred);
    }

    protected sealed class effect_provider(Func<SemanticImplementationBody, SemanticReactionContext, SemanticReactionBodyResult> react) : ISemanticBodyExecutionProvider
    {
        internal Exception? Failure;
        internal int Calls;
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask<SemanticReactionBodyResult> ReactAsync(SemanticImplementationBody body, SemanticReactionContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Calls++;
            if (Failure is { } failure) throw failure;
            return new(react(body, context));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
