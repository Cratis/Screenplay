// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_reaction_effect_provider : Specification
{
    const string Source = """
        policy Allowed
          ```csharp
          return true;
          ```
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
                authorize Allowed
                validate
                  ```csharp
                  yield break;
                  ```
                produces Recorded
                  for name
                  name = name
                handler
                  ```csharp
                  return payload;
                  ```
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
                when append Requested
                  for "stream"
                  name = "First"
                then Recorded
                  for "First"
                  name = "First"
        """;

    string _root;
    effect_factory _factory;
    JsonElement _enabled;
    JsonElement _default;

    void Establish()
    {
        _factory = new();
        _root = SpecFixtureDirectory.Create("reaction-provider");
        File.WriteAllText(Path.Combine(_root, "orders.play"), Source);
    }
    void Because()
    {
        _default = Run(null);
        _enabled = Run(_factory);
    }
    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Fact] void should_keep_the_default_opaque() => _default.GetProperty("unsupported").GetInt32().ShouldEqual(1);
    [Fact] void should_satisfy_the_specification() => _enabled.GetProperty("passed").GetInt32().ShouldEqual(1);
    [Fact] void should_have_no_unsupported_run() => _enabled.GetProperty("unsupported").GetInt32().ShouldEqual(0);
    [Fact] void should_reuse_one_provider() => _factory.Providers.Count.ShouldEqual(1);
    [Fact]
    void should_resolve_all_roles() => _factory.Providers.Single().Calls.ShouldEqual([
        SemanticImplementationRole.ReactionEffect, SemanticImplementationRole.PolicyPredicate,
        SemanticImplementationRole.CommandValidation, SemanticImplementationRole.CommandHandler, SemanticImplementationRole.ReducerTransition]);
    [Fact] void should_dispose_once_at_run_end() => _factory.Providers.Single().Disposals.ShouldEqual(1);

    JsonElement Run(ISemanticBodyExecutionProviderFactory? factory)
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
        var result = response.RootElement.GetProperty("result").GetProperty("structuredContent");
        Assert.True(result.GetProperty("passed").GetInt32() + result.GetProperty("unsupported").GetInt32() == 1, result.GetRawText());
        return result.Clone();
    }

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    sealed class effect_factory : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<effect_provider> Providers = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            bodies.Count.ShouldEqual(5);
            bodies.Values.Any(body => body.Content == "return commands;").ShouldBeTrue();
            var slice = application.Modules.Single().Features.Single().Slices.Single();
            var provider = new effect_provider(slice.Commands.Single(command => command.Name == "Record"), slice.Events.Single(contract => contract.Name == "Recorded"), slice.ReadModels.Single());
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class effect_provider(SemanticCommand command, SemanticEventContract contract, SemanticReadModel model) : ISemanticBodyExecutionProvider
    {
        internal readonly List<SemanticImplementationRole> Calls = [];
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticReactionBodyResult> ReactAsync(SemanticImplementationBody body, SemanticReactionContext context, CancellationToken cancellationToken)
        {
            Called(body);
            return new(SemanticReactionBodyResult.Invoked([new(command.Id, SemanticValue.Composite([new(command.Properties.Single().Id, context.Event.Properties.Single().Value)]))]));
        }
        public ValueTask<SemanticPolicyBodyResult> AuthorizeAsync(SemanticImplementationBody body, SemanticPolicyContext context, CancellationToken cancellationToken)
        {
            Called(body);
            return new(SemanticPolicyBodyResult.Allow());
        }
        public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            Called(body);
            return new(SemanticValidationBodyResult.Valid());
        }
        public ValueTask<SemanticCommandBodyResult> HandleAsync(SemanticImplementationBody body, SemanticCommandContext context, CancellationToken cancellationToken)
        {
            Called(body);
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
