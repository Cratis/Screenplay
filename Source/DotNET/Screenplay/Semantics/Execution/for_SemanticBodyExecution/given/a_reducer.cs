// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.given;

public class a_reducer : Specification
{
    protected const string Body = "return context.State;";
    protected const string Source = """
        module Accounts
          feature Balances
            slice StateChange Deposits
              command Deposit
                accountId String identifier
                produces AmountDeposited
                  for accountId
                  accountId = accountId
              event AmountDeposited
                accountId String
            slice StateView BalanceView
              readmodel Balance
                accountId String key
              query BalanceById => Balance optional
                by accountId String
              reducer BalanceReducer => Balance
                on AmountDeposited
                  ```csharp
                  return context.State;
                  ```
        """;

    protected SemanticExecutionPlan Plan = null!;
    protected ImmutableArray<SemanticImplementationBody> Bodies;
    protected reducer_provider Provider = null!;
    protected List<SemanticReducerContext> Contexts = [];
    protected SemanticValue Key = SemanticValue.Text("first");
    protected SemanticReducerBodyResult? Answer;
    protected SemanticReadModel ReadModel => Plan.ReadModels.Values.Single();
    protected SemanticEvaluator Evaluator => new(Provider, Bodies);
    protected SemanticValue State => SemanticValue.Composite([new(ReadModel.Properties.Single().Id, Key)]);
    protected ImmutableArray<SemanticQueryRequest> Queries => [new(Plan.Queries.Values.Single().Id, Key)];

    void Establish() => Reset();

    protected void Reset()
    {
        Contexts.Clear();
        Answer = null;
        Key = SemanticValue.Text("first");
        Configure(Source);
        Provider = new(context =>
        {
            Contexts.Add(context);
            return Answer ?? SemanticReducerBodyResult.Next(State);
        });
    }

    protected void Configure(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Accounts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("accounts"), "accounts", "accounts.play", source);
        var compilation = new SemanticModelCompiler().Compile("Accounts", SemanticDocumentSet.Create([document], catalog));
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
        Plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        Bodies = [.. compilation.ImplementationRequirements.Select(requirement => new SemanticImplementationBody(requirement, Body))];
    }

    protected static SemanticAccepted Accept(SemanticExecutionResult result)
    {
        result.ShouldBeOfExactType<SemanticAccepted>();
        return (SemanticAccepted)result;
    }

    protected void UseKey(SemanticValue key) => Key = key;
    protected void UseAnswer(SemanticReducerBodyResult? answer) => Answer = answer;
    protected void UseProvider(reducer_provider provider) => Provider = provider;

    protected SemanticFact Fact() => new(Plan.Events.Values.Single().Id, Key, [new(Plan.Events.Values.Single().Properties.Single().Id, Key)])
    {
        Context = new(new(SemanticTypeReference.ForPrimitive(Key is SemanticNumberValue ? SemanticPrimitiveType.WholeNumber : SemanticPrimitiveType.Text), Key)),
        Occurred = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)
    };

    protected SemanticExecutionRequest Request() => SemanticExecutionRequest.Create(Plan.Commands.Values.Single().Id, [new(Plan.Commands.Values.Single().Properties.Single().Id, Key)], Queries);

    protected sealed class reducer_provider(Func<SemanticReducerContext, SemanticReducerBodyResult> reduce) : ISemanticBodyExecutionProvider
    {
        internal Exception? Failure;
        internal Action<CancellationToken>? Before;
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
        {
            if (Disposals > 0) throw new InvalidOperationException("Provider already disposed.");
            Before?.Invoke(cancellationToken);
            if (Failure is { } failure) throw failure;
            return new(reduce(context));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }

    protected sealed class reducer_factory(reducer_provider provider) : ISemanticBodyExecutionProviderFactory
    {
        internal Exception? Failure;
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            if (Failure is { } failure) throw failure;
            return new(provider);
        }
    }
}
