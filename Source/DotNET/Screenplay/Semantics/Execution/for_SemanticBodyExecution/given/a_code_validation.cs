// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_code_validation : Specification
{
    internal const string Body = "yield break;";
    internal const string Source = """
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                validate
                  ```csharp
                  yield break;
                  ```
                produces Placed
                  for name
              event Placed
        """;

    protected validation_provider Provider = null!;
    protected SemanticExecutionPlan Plan = null!;
    protected SemanticExecutionRequest Request = null!;
    protected SemanticExecutionResult Result = null!;
    protected SemanticValidationBodyResult Answer = SemanticValidationBodyResult.Valid();
    protected ImmutableArray<SemanticImplementationBody> Bodies;
    protected List<SemanticRuleContext> Contexts = [];
    protected List<SemanticImplementationBody> ReceivedBodies = [];

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

    protected void Configure(string source, params string[] expectedDiagnostics)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("validations"), "validations", "orders.play", source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(expectedDiagnostics);
        Plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Bodies = [.. compilation.ImplementationRequirements.Select(requirement => new SemanticImplementationBody(requirement, Body))];
        var command = Plan.Commands.Values.Single();
        Request = SemanticExecutionRequest.Create(command.Id, [.. command.Properties.Where(property => !property.IsGenerated).Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text("First")))], []) with
        {
            Occurrence = new(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero), "subject", "Name", "user"),
            Tenant = "tenant"
        };
    }

    protected SemanticExecutionResult Execute() => Execute(CancellationToken.None);

    protected SemanticExecutionResult Execute(CancellationToken cancellationToken) =>
        new SemanticEvaluator(Provider, Bodies).ExecuteWithCancellation(Plan, SemanticWorld.Empty, Request, cancellationToken);

    protected sealed class validation_provider(Func<SemanticImplementationBody, SemanticRuleContext, SemanticValidationBodyResult> validate) : ISemanticBodyExecutionProvider
    {
        internal Exception? Failure { get; set; }
        internal Action<CancellationToken>? BeforeValidation { get; set; }
        internal SemanticBodyExecutionResult PredicateAnswer { get; set; } = new(true);

        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => new(PredicateAnswer);

        public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            BeforeValidation?.Invoke(cancellationToken);
            if (Failure is { } failure) throw failure;
            return new(validate(body, context));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
