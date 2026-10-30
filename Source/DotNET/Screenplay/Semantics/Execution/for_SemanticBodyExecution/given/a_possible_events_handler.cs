// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_possible_events_handler : Specification
{
    protected const string Source = """
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                targets String[]
                produces Placed
                  for name
                  tag production
                produces Recorded
                  for name
                produces Noted
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
              event Noted
                name String
        """;

    protected SemanticExecutionPlan Plan = null!;
    protected SemanticExecutionRequest Request = null!;
    protected SemanticExecutionResult Result = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;
    protected SemanticCommandBodyResult Answer = null!;
    protected handler_provider Provider = null!;
    protected SemanticCommand Command => Plan.Commands.Values.Single();
    protected SemanticId Name => Command.Properties.Single(property => property.Name == "name").Id;
    protected SemanticId Targets => Command.Properties.Single(property => property.Name == "targets").Id;
    protected IReadOnlyDictionary<SemanticId, SemanticValue> Values => Request.Values.ToDictionary(value => value.TargetProperty, value => value.Value);

    void Establish() => Configure(Source);

    protected void Configure(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("handlers"), "handlers", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        var planned = SemanticExecutionPlan.Compile(compilation.Value.Model);
        planned.Issues.ShouldBeEmpty();
        Plan = planned.Plan;
        Bodies = [.. compilation.ImplementationRequirements.Select(requirement => new SemanticImplementationBody(requirement with { ResultVersion = 2 }, "return payloads;"))];
        ChangeCommand(command => command with
        {
            Produces = [],
            Handler = new(command.Handler.RequirementId, 2)
            {
                PossibleEvents = [.. command.Produces.Select(produced => Plan.Events[produced.EventContract].Name switch
                {
                    "Recorded" => produced with { Destination = new SemanticOneOfExpression(Targets) },
                    "Noted" => produced with { Destination = null },
                    _ => produced
                })]
            }
        });
        Request = SemanticExecutionRequest.Create(Command.Id, [new(Name, SemanticValue.Text("First")), new(Targets, SemanticValue.Array([SemanticValue.Text("A"), SemanticValue.Text("B")]))], []) with
        {
            Tenant = "tenant",
            Caller = new(true, ["Member"], []),
            Occurrence = new(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero), "audit", "Name", "user")
        };
        Answer = SemanticCommandBodyResult.ProducedEvents([]);
        Provider = new(() => Answer);
    }

    protected SemanticCommandBodyItem Item(string name, string? destination = null)
    {
        var contract = Plan.Events.Values.Single(value => value.Name == name);
        return new(contract.Id, SemanticValue.Composite([new(contract.Properties[0].Id, SemanticValue.Text("Handled"))]), destination is null ? null : SemanticValue.Text(destination));
    }

    protected void ChangeCommand(Func<SemanticCommand, SemanticCommand> change)
    {
        var command = Command;
        Plan = new(
            Plan.Model, Plan.Commands.SetItem(command.Id, change(command)), Plan.Events, Plan.Projections, Plan.ReadModels, Plan.Queries, Plan.Specifications, Plan.Constraints, Plan.Reactions, Plan.Captures, Plan.UnadmittedArtifacts);
    }

    protected SemanticExecutionResult Execute() => new SemanticEvaluator(Provider, Bodies).ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, TestContext.Current.CancellationToken);

    protected sealed class handler_provider(Func<SemanticCommandBodyResult> handle) : ISemanticBodyExecutionProvider
    {
        internal Exception? Failure;
        internal int Calls;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken)
        {
            Calls++;
            if (Failure is { } failure) throw failure;
            return new(handle());
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
