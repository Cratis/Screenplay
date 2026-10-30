// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_policy : Specification
{
    internal const string Body = "return true;";
    internal const string Source = """
        policy Permitted
          ```csharp
          return true;
          ```
        policy Member
          require authenticated
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                authorize Permitted
                produces Placed
                  for name
              event Placed
            slice StateView Orders
              readmodel Order
                name String key
              query OrderByName => Order optional
                by name String
                authorize Permitted
        """;

    protected SemanticExecutionPlan Plan = null!;
    protected SemanticExecutionRequest Request = null!;
    protected SemanticExecutionResult Result = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;
    protected policy_provider Provider = null!;
    protected List<SemanticPolicyContext> Contexts = [];
    protected List<SemanticImplementationBody> ReceivedBodies = [];
    protected SemanticPolicyBodyResult Answer = SemanticPolicyBodyResult.Allow();

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
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("policies"), "policies", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        Plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Bodies = [.. compilation.ImplementationRequirements.Select(requirement => new SemanticImplementationBody(requirement, Body))];
        var command = Plan.Commands.Values.Single();
        Request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single(property => !property.IsGenerated).Id, SemanticValue.Text("First"))], []) with
        {
            Caller = new(true, ["Member"], []),
            Tenant = "tenant",
            Occurrence = new(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero), "audit", "Name", "user")
        };
    }

    protected SemanticExecutionResult Execute() => Execute(TestContext.Current.CancellationToken);

    protected SemanticExecutionResult Execute(CancellationToken cancellationToken) =>
        new SemanticEvaluator(Provider, Bodies).ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, cancellationToken);

    protected SemanticExecutionRequest QueryRequest() => SemanticExecutionRequest.ForQueries([new(Plan.Queries.Values.Single().Id, SemanticValue.Text("First"))]) with
    {
        Caller = Request.Caller,
        Tenant = Request.Tenant,
        Occurrence = Request.Occurrence
    };

    protected sealed class policy_provider(Func<SemanticImplementationBody, SemanticPolicyContext, SemanticPolicyBodyResult> authorize) : ISemanticBodyExecutionProvider
    {
        internal Action<CancellationToken>? Before { get; set; }
        internal Exception? Failure { get; set; }
        internal int Disposals { get; private set; }

        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(new SemanticBodyExecutionResult(true));
        public ValueTask<SemanticPolicyBodyResult> AuthorizeAsync(SemanticImplementationBody body, SemanticPolicyContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Before?.Invoke(cancellationToken);
            if (Failure is { } failure) throw failure;
            return new(authorize(body, context));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
