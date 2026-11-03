// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_reaction_effect_provider_with_denied_commands : Specification
{
    const string Source = """
        policy Restricted
          require role "Administrator"
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
              command RecordFirst
                name String
                produces Recorded
                  for name
                handler
                  ```csharp
                  return payload;
                  ```
              command Record
                name String
                authorize Restricted
                produces Recorded
                  for name
                  name = name
              reaction OnRequested
                runs as system role "Recorder"
                when Requested
                  name
                  ```csharp
                  return commands;
                  ```
              readmodel Result
                name String key
              reducer Fold => Result
                on Recorded
                  ```csharp
                  return state;
                  ```
              specification Followed
                given caller
                  authenticated
                  role "Administrator"
                when append Requested
                  for "stream"
                  name = "First"
                then denied
        """;
    const string Invocations = "          invokes RecordFirst\n            name = \"First\"\n          invokes Record\n            name = \"Denied\"\n          invokes RecordFirst\n            name = \"Last\"";

    string _root = null!;
    void Establish() => _root = SpecFixtureDirectory.Create("reaction-provider-denied");
    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData("runs as system role \"Recorder\"")]
    [InlineData("")]
    [InlineData("runs as system")]
    void should_stop_like_invokes_without_on_refused_without_claiming_an_atomic_batch(string identity)
    {
        var source = Source.Replace("runs as system role \"Recorder\"", identity, StringComparison.Ordinal);
        var bodyFactory = new effect_factory();
        var bodyReport = Run(source, bodyFactory);
        var invokesFactory = new effect_factory();
        var invokesReport = Run(source.Replace("          ```csharp\n          return commands;\n          ```", Invocations, StringComparison.Ordinal), invokesFactory);

        bodyReport.GetProperty("passed").GetInt32().ShouldEqual(1);
        bodyReport.GetProperty("unsupported").GetInt32().ShouldEqual(0);
        invokesReport.GetProperty("passed").GetInt32().ShouldEqual(1);
        bodyReport.GetProperty("page").GetProperty("items")[0].GetProperty("executionOutcome").GetString().ShouldEqual(invokesReport.GetProperty("page").GetProperty("items")[0].GetProperty("executionOutcome").GetString());
        var provider = bodyFactory.Providers.Single();
        provider.Handled.ShouldEqual([SemanticValue.Text("First")]);
        provider.Settled.ShouldEqual([SemanticValue.Text("First")]);
        provider.Handled.ShouldEqual(invokesFactory.Providers.Single().Handled);
        provider.Settled.ShouldEqual(invokesFactory.Providers.Single().Settled);
        AssertIdentity(provider.RunsAs, identity);
        provider.Disposals.ShouldEqual(1);
    }

    static void AssertIdentity(SemanticReactionIdentity? actual, string identity)
    {
        if (identity.Length == 0)
        {
            actual.ShouldBeNull();
            return;
        }
        actual!.Kind.ShouldEqual(SemanticReactionIdentityKind.System);
        actual.Roles.ShouldEqual(identity.Contains("Recorder", StringComparison.Ordinal) ? ["Recorder"] : Array.Empty<string>());
    }

    JsonElement Run(string source, ISemanticBodyExecutionProviderFactory factory)
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), source);
        var lines = new[]
        {
            Request(1, "initialize", new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "spec", version = "1" } }),
            JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "notifications/initialized" }),
            Request(2, "tools/call", new { name = "open-workspace", arguments = new { } }),
            Request(3, "tools/call", new { name = "run-specifications", arguments = new { } })
        };
        using var input = new StringReader(string.Join('\n', lines));
        using var output = new StringWriter();
        ScreenplayMcpServer.RunConfigured(new ScreenplayMcpRun(_root, factory, TestContext.Current.CancellationToken), input, output);
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    sealed class effect_factory : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<effect_provider> Providers = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            var slice = application.Modules.Single().Features.Single().Slices.Single();
            var provider = new effect_provider(slice.Commands.Single(command => command.Name == "RecordFirst"), slice.Commands.Single(command => command.Name == "Record"), slice.Events.Single(contract => contract.Name == "Recorded"), slice.ReadModels.Single());
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class effect_provider(SemanticCommand first, SemanticCommand denied, SemanticEventContract contract, SemanticReadModel model) : ISemanticBodyExecutionProvider
    {
        internal readonly List<SemanticValue> Handled = [];
        internal readonly List<SemanticValue> Settled = [];
        internal SemanticReactionIdentity? RunsAs;
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticReactionBodyResult> ReactAsync(SemanticImplementationBody body, SemanticReactionContext context, CancellationToken cancellationToken)
        {
            RunsAs = context.RunsAs;
            return new(SemanticReactionBodyResult.Invoked([
                new(first.Id, SemanticValue.Composite([new(first.Properties.Single().Id, SemanticValue.Text("First"))])),
                new(denied.Id, SemanticValue.Composite([new(denied.Properties.Single().Id, SemanticValue.Text("Denied"))])),
                new(first.Id, SemanticValue.Composite([new(first.Properties.Single().Id, SemanticValue.Text("Last"))]))]));
        }
        public ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken)
        {
            var value = context.Command.Properties.Single().Value;
            Handled.Add(value);
            return new(SemanticCommandBodyResult.Produced([SemanticValue.Composite([new(contract.Properties.Single().Id, value)])]));
        }
        public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
        {
            Settled.Add(context.Key);
            return new(SemanticReducerBodyResult.Next(SemanticValue.Composite([new(model.Properties.Single().Id, context.Key)])));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
