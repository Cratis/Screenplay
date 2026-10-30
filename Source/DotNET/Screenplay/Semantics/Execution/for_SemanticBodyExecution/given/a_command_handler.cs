// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_command_handler : Specification
{
    internal const string Body = "return payloads;";
    internal const string Source = """
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                produces Placed
                  for name
                  tag production
                produces Recorded
                  for name
                handler
                  ```csharp
                  return payloads;
                  ```
              event Placed
                tag persisted
                name String
                label String optional
              event Recorded
                name String
            slice StateView Orders
              readmodel Order
                name String key
              query OrderByName => Order optional
                by name String
              projection Orders => Order
                from Placed
                  key name
                  name = name
        """;

    protected SemanticExecutionPlan Plan = null!;
    protected SemanticExecutionRequest Request = null!;
    protected SemanticExecutionResult Result = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;
    protected handler_provider Provider = null!;
    protected List<SemanticCommandContext> Contexts = [];
    protected List<SemanticImplementationBody> ReceivedBodies = [];
    protected SemanticCommandBodyResult Answer = null!;
    protected ImmutableArray<SemanticValue> Payloads;

    void Establish()
    {
        Configure(Source);
        Provider = new((body, context) =>
        {
            ReceivedBodies.Add(body);
            Contexts.Add(context);
            return Answer;
        });
    }

    protected void Configure(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("handlers"), "handlers", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        var planned = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        planned.Issues.ShouldBeEmpty();
        Plan = planned.Plan!;
        Bodies = [.. compilation.ImplementationRequirements.Select(requirement => new SemanticImplementationBody(requirement, requirement.Role == SemanticImplementationRole.CommandHandler ? Body : "return context.State;"))];
        var command = Plan.Commands.Values.Single();
        Request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single(property => !property.IsGenerated).Id, SemanticValue.Text("First"))], []) with
        {
            Tenant = "tenant",
            Caller = new(true, ["Member"], []),
            Occurrence = new(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero), "audit", "Name", "user")
        };
        Payloads = [.. command.Produces.Select(produced => SemanticValue.Composite(
            [.. Plan.Events[produced.EventContract].Properties.Take(1).Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text("Handled")))]))];
        Answer = SemanticCommandBodyResult.Produced(Payloads);
    }

    protected void ChangeCommand(Func<SemanticCommand, SemanticCommand> change)
    {
        var command = Plan.Commands.Values.Single();
        Plan = new(Plan.Model, Plan.Commands.SetItem(command.Id, change(command)), Plan.Events, Plan.Projections, Plan.ReadModels,
            Plan.Queries, Plan.Specifications, Plan.Constraints, Plan.Reactions, Plan.Captures, Plan.UnadmittedArtifacts);
    }

    protected SemanticExecutionResult Execute() => Execute(TestContext.Current.CancellationToken);

    protected SemanticExecutionResult Execute(CancellationToken cancellationToken) =>
        new SemanticEvaluator(Provider, Bodies).ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, cancellationToken);

    protected sealed class handler_provider(Func<SemanticImplementationBody, SemanticCommandContext, SemanticCommandBodyResult> handle) : ISemanticBodyExecutionProvider
    {
        internal Exception? Failure;
        internal Action<CancellationToken>? Before;
        internal int Disposals;
        internal int HandleCalls;
        internal Func<SemanticReducerContext, SemanticReducerBodyResult>? Reduce;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            HandleCalls++;
            Before?.Invoke(cancellationToken);
            if (Failure is { } failure) throw failure;
            return new(handle(body, context));
        }
        public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            return new(Reduce?.Invoke(context) ?? SemanticReducerBodyResult.Unsupported("No reducer."));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
