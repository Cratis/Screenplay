// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_rule : Specification
{
    protected const string Body = "return context.Value != null;";
    protected const string Source = """
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                validate
                  name rule valid message "A name is required"
                    ```csharp
                    return context.Value != null;
                    ```
                produces Placed
                  for name
              event Placed
        """;

    protected rule_provider Provider = null!;
    protected SemanticExecutionPlan Plan = null!;
    protected SemanticExecutionRequest Request = null!;
    protected SemanticImplementationRequirement Requirement = null!;
    protected SemanticBodyExecutionResult Answer = new(true);
    protected SemanticRuleContext? Context;
    protected SemanticImplementationBody? ReceivedBody;
    protected SemanticExecutionResult Result = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;

    void Establish()
    {
        Configure(Source);
        Provider = new rule_provider((body, context) =>
        {
            ReceivedBody = body;
            Context = context;
            return Answer;
        });
    }

    protected void Configure(string source, params string[] expectedDiagnostics)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("rules"), "rules", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Count().ShouldEqual(expectedDiagnostics.Length);
        foreach (var diagnostic in compilation.Diagnostics) expectedDiagnostics.ShouldContain(diagnostic.Code);
        Plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Requirement = compilation.ImplementationRequirements.Single();
        Bodies = [new(Requirement, Body)];
        var command = Plan.Commands.Values.Single();
        Request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("First"))], []) with
        {
            Occurrence = new(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero), "subject", "Name", "user"),
            Tenant = "tenant"
        };
    }

    protected sealed class rule_provider(Func<SemanticImplementationBody, SemanticRuleContext, SemanticBodyExecutionResult> evaluate) : ISemanticBodyExecutionProvider
    {
        internal Exception? Failure { get; set; }
        internal Action<CancellationToken>? BeforeEvaluation { get; set; }
        internal CancellationToken Cancellation { get; private set; }
        internal int Disposals { get; private set; }

        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            Cancellation = cancellationToken;
            BeforeEvaluation?.Invoke(cancellationToken);
            if (Failure is { } failure) throw failure;
            return new(evaluate(body, context));
        }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
