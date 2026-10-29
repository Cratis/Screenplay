// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_policy_provider : Specification
{
    const string Body = "return true;";
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
                produces Placed
                  for name
              event Placed
              specification Allowed
                given caller
                  authenticated
                when Place
                  name = "First"
                then Placed
        """;

    string _root = null!;
    void Establish() => _root = SpecFixtureDirectory.Create("policy-provider");
    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_resolve_policy_source_and_dispose_the_run(bool file)
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), file ? Source.Replace("  ```csharp\n  return true;\n  ```", "  file Permitted.cs", StringComparison.Ordinal) : Source);
        File.WriteAllText(Path.Combine(_root, "Permitted.cs"), Body);
        var factory = new policy_factory();
        Run(factory).GetProperty("passed").GetInt32().ShouldEqual(1);
        var provider = factory.Providers.Single();
        provider.Body!.Content.ShouldEqual(Body);
        provider.Body.Requirement.Role.ShouldEqual(SemanticImplementationRole.PolicyPredicate);
        (provider.Body.Requirement.File is not null).ShouldEqual(file);
        (provider.Body.Requirement.Language is null).ShouldEqual(file);
        provider.Context!.Subject.ShouldEqual("First");
        provider.Disposals.ShouldEqual(1);
    }

    [Fact]
    void should_not_mistake_an_unsupported_policy_for_then_denied()
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), Source.Replace("then Placed", "then denied", StringComparison.Ordinal));
        var report = Run(null);
        report.GetProperty("passed").GetInt32().ShouldEqual(0);
        report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
    }

    [Fact]
    void should_not_mistake_a_provider_unsupported_answer_for_then_denied()
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), Source.Replace("then Placed", "then denied", StringComparison.Ordinal));
        var factory = new policy_factory(true);
        var report = Run(factory);
        report.GetProperty("passed").GetInt32().ShouldEqual(0);
        report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
        factory.Providers.Single().Disposals.ShouldEqual(1);
    }

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
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    sealed class policy_factory(bool unsupported = false) : ISemanticBodyExecutionProviderFactory
    {
        internal readonly List<policy_provider> Providers = [];
        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            bodies.Values.Single().Requirement.Role.ShouldEqual(SemanticImplementationRole.PolicyPredicate);
            var provider = new policy_provider(unsupported);
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class policy_provider(bool unsupported) : ISemanticBodyExecutionProvider
    {
        internal SemanticImplementationBody? Body;
        internal SemanticPolicyContext? Context;
        internal int Disposals;
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public ValueTask<SemanticPolicyBodyResult> AuthorizeAsync(SemanticImplementationBody body, SemanticPolicyContext context, CancellationToken cancellationToken)
        {
            if (Disposals != 0) throw new InvalidOperationException("Disposed provider.");
            Body = body;
            Context = context;
            return new(unsupported ? SemanticPolicyBodyResult.Unsupported("not evaluable") : SemanticPolicyBodyResult.Allow());
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
