// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_reducer_provider : Specification
{
    const string Body = "return context.State;";
    const string Source = """
        module Accounts
          feature Balances
            slice StateChange Deposits
              command Deposit
                accountId String identifier
                validate
                  ```csharp
                  yield break;
                  ```
                produces Deposited
                  for accountId
              event Deposited
              specification DepositRecorded
                when Deposit
                  accountId = "first"
                then Deposited
                then readmodel Balance
                  accountId = "first"
                then query BalanceById
                  arguments
                    accountId = "first"
                  result
                    accountId = "first"
            slice StateView Balances
              readmodel Balance
                accountId String key
              query BalanceById => Balance optional
                by accountId String
              reducer FoldBalance => Balance
                on Deposited
                  ```csharp
                  return context.State;
                  ```
        """;

    string _root = null!;

    void Establish() => _root = SpecFixtureDirectory.Create("reducer-provider");
    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_resolve_inline_and_file_reducers_in_one_live_run_scope(bool file)
    {
        WriteModel(file);
        var factory = new reducer_factory();
        Run(factory).GetProperty("passed").GetInt32().ShouldEqual(1);
        var provider = factory.Providers.Single();
        provider.Validations.ShouldEqual(1);
        provider.Body!.Content.ShouldEqual(Body);
        provider.Body.Requirement.Role.ShouldEqual(SemanticImplementationRole.ReducerTransition);
        (provider.Body.Requirement.File is not null).ShouldEqual(file);
        provider.Context!.Key.ShouldEqual(SemanticValue.Text("first"));
        provider.Context.SequenceNumber.ShouldEqual(0UL);
        provider.Context.State.ShouldBeNull();
        provider.Disposals.ShouldEqual(1);
    }

    [Fact]
    void should_preserve_the_default_host_refusal()
    {
        WriteModel(false);
        var report = Run(null);
        report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
        report.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString().ShouldContain("Reducer 'FoldBalance'");
    }

    void WriteModel(bool file)
    {
        File.WriteAllText(Path.Combine(_root, "accounts.play"), file ? Source.Replace("          ```csharp\n          return context.State;\n          ```", "          file Reduce.cs", StringComparison.Ordinal) : Source);
        File.WriteAllText(Path.Combine(_root, "Reduce.cs"), Body);
    }

    JsonElement Run(ISemanticBodyExecutionProviderFactory? provider)
    {
        var lines = new[]
        {
            Request(1, "initialize", new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "spec", version = "1" } }),
            JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "notifications/initialized" }),
            Request(2, "tools/call", new { name = "open-workspace", arguments = new { } }),
            Request(3, "tools/call", new { name = "run-specifications", arguments = new { } })
        };
        using var input = new StringReader(string.Join('\n', lines));
        using var output = new StringWriter();
        ScreenplayMcpServer.RunConfigured(new ScreenplayMcpRun(_root, provider, TestContext.Current.CancellationToken), input, output);
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    sealed class reducer_factory : ISemanticBodyExecutionProviderFactory
    {
        internal List<reducer_provider> Providers { get; } = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            var model = application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.ReadModels).Single();
            var provider = new reducer_provider(model.Properties.Single().Id);
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class reducer_provider(SemanticId property) : ISemanticBodyExecutionProvider
    {
        internal SemanticImplementationBody? Body { get; private set; }
        internal SemanticReducerContext? Context { get; private set; }
        internal int Validations { get; private set; }
        internal int Disposals { get; private set; }

        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Validations++;
            return new(SemanticValidationBodyResult.Valid());
        }
        public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Body = body;
            Context = context;
            return new(SemanticReducerBodyResult.Next(SemanticValue.Composite([new(property, context.Key)])));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
