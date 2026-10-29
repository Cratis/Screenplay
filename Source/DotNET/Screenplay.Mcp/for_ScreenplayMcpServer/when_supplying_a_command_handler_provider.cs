// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_command_handler_provider : Specification
{
    const string Body = "return payload;";
    const string Source = """
        policy Permitted
          ```csharp
          return true;
          ```
        module Orders
          feature Ordering
            slice StateChange Place
              command Place
                name String identifier
                authorize Permitted
                validate
                  ```csharp
                  yield break;
                  ```
                produces Placed
                  for name
                handler
                  ```csharp
                  return payload;
                  ```
              event Placed
                name String
              specification PlacedSuccessfully
                given caller
                  authenticated
                when Place
                  name = "First"
                then Placed
                  name = "First"
                then readmodel Order
                  name = "First"
                then query OrderByName
                  arguments
                    name = "First"
                  result
                    name = "First"
            slice StateView Orders
              readmodel Order
                name String key
              query OrderByName => Order optional
                by name String
                authorize Permitted
              reducer FoldOrders => Order
                on Placed
                  ```csharp
                  return context.State;
                  ```
        """;

    string _root = null!;
    void Establish() => _root = SpecFixtureDirectory.Create("handler-provider");
    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_share_one_live_run_for_validation_policy_handler_and_reducer(bool file)
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), file ? Source.Replace("          ```csharp\n          return payload;\n          ```", "          file Handle.cs", StringComparison.Ordinal) : Source);
        File.WriteAllText(Path.Combine(_root, "Handle.cs"), Body);
        var factory = new handler_factory();
        Run(factory).GetProperty("passed").GetInt32().ShouldEqual(1);
        var provider = factory.Providers.Single();
        provider.Calls.ShouldContain(SemanticImplementationRole.CommandValidation);
        provider.Calls.ShouldContain(SemanticImplementationRole.PolicyPredicate);
        provider.Calls.ShouldContain(SemanticImplementationRole.CommandHandler);
        provider.Calls.ShouldContain(SemanticImplementationRole.ReducerTransition);
        provider.Handler!.Content.ShouldEqual(Body);
        (provider.Handler.Requirement.File is not null).ShouldEqual(file);
        provider.Disposals.ShouldEqual(1);
    }

    [Fact]
    void should_resolve_changed_file_content_in_a_new_run_not_a_cached_body()
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), Source.Replace("          ```csharp\n          return payload;\n          ```", "          file Handle.cs", StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(_root, "Handle.cs"), Body);
        var factory = new handler_factory();
        Run(factory).GetProperty("passed").GetInt32().ShouldEqual(1);
        File.WriteAllText(Path.Combine(_root, "Handle.cs"), "changed source");
        Run(factory).GetProperty("passed").GetInt32().ShouldEqual(1);
        factory.Providers.Count.ShouldEqual(2);
        factory.Providers[1].Handler!.Content.ShouldEqual("changed source");
        factory.Providers.All(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    JsonElement Run(ISemanticBodyExecutionProviderFactory factory)
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
        ScreenplayMcpServer.RunConfigured(new ScreenplayMcpRun(_root, factory, TestContext.Current.CancellationToken), input, output);
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    sealed class handler_factory : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<handler_provider> Providers = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            bodies.Count.ShouldEqual(4);
            var slices = application.Modules.Single().Features.Single().Slices;
            var contract = slices.SelectMany(slice => slice.Events).Single();
            var model = slices.SelectMany(slice => slice.ReadModels).Single();
            var provider = new handler_provider(contract, model);
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class handler_provider(SemanticEventContract contract, SemanticReadModel model) : ISemanticBodyExecutionProvider
    {
        internal readonly List<SemanticImplementationRole> Calls = [];
        internal SemanticImplementationBody? Handler;
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            Called(body);
            return new(SemanticValidationBodyResult.Valid());
        }
        public ValueTask<SemanticPolicyBodyResult> AuthorizeAsync(SemanticImplementationBody body, SemanticPolicyContext context, CancellationToken cancellationToken)
        {
            Called(body);
            return new(SemanticPolicyBodyResult.Allow());
        }
        public ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken)
        {
            Called(body);
            Handler = body;
            return new(SemanticCommandBodyResult.Produced([SemanticValue.Composite([new(contract.Properties.Single().Id, context.Command.Properties.Single().Value)])]));
        }
        public ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
        {
            Called(body);
            return new(SemanticReducerBodyResult.Next(SemanticValue.Composite([new(model.Properties.Single().Id, context.Key)])));
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
        void Called(SemanticImplementationBody body)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Calls.Add(body.Requirement.Role);
        }
    }
}
