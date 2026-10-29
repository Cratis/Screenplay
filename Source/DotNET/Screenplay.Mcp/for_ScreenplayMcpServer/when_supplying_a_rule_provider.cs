// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_rule_provider : Specification
{
    const string Body = "return context.Value != null;";
    const string Source = """
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
              specification Valid
                given clock "2030-01-02T03:04:05Z"
                when Place
                  name = "First"
                then Placed
                  for "First"
        """;

    string _root = null!;
    rule_factory _factory = null!;

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    void Establish()
    {
        _root = SpecFixtureDirectory.Create("rule-provider");
        _factory = new();
    }

    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_resolve_inline_and_file_bodies_through_the_public_host(bool file)
    {
        WriteModel(file);
        var report = Run(_factory);
        report.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString().ShouldBeNull();
        report.GetProperty("passed").GetInt32().ShouldEqual(1);
        var provider = _factory.Providers.Single();
        provider.Body!.Content.ShouldEqual(Body);
        (provider.Body.Requirement.File is not null).ShouldEqual(file);
        provider.Context!.Value.ShouldEqual(SemanticValue.Text("First"));
        provider.Context.Occurred.ShouldEqual(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        provider.Context.CausedBy.ShouldBeNull();
        provider.Disposals.ShouldEqual(1);
        _factory.Bodies![provider.Body.Requirement.RequirementId].ShouldEqual(provider.Body);
        _factory.Application!.Modules.Single().Name.ShouldEqual("Orders");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_no_provider_output_unchanged(bool file)
    {
        WriteModel(file);
        var report = Run(null);
        report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
        report.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString().ShouldEqual("Rule 'valid' on command 'Place' property 'name' has an opaque validation predicate and requires a target provider.");
    }

    [Fact]
    void should_create_and_dispose_an_isolated_provider_per_run()
    {
        WriteModel(false);
        Run(_factory, runs: 2).GetProperty("passed").GetInt32().ShouldEqual(1);
        _factory.Providers.Count.ShouldEqual(2);
        _factory.Providers.TrueForAll(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    [Fact]
    void should_propagate_host_cancellation_and_dispose()
    {
        WriteModel(false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Run(_factory, cancellation.Token).GetProperty("unsupported").GetInt32().ShouldEqual(1);
        _factory.Cancellation.ShouldEqual(cancellation.Token);
        _factory.Providers.Single().Disposals.ShouldEqual(1);
    }

    void WriteModel(bool file)
    {
        var source = file ? Source.Replace("            ```csharp\n            return context.Value != null;\n            ```", "            file Rule.cs", StringComparison.Ordinal) : Source;
        File.WriteAllText(Path.Combine(_root, "orders.play"), source);
        File.WriteAllText(Path.Combine(_root, "Rule.cs"), Body);
    }

    JsonElement Run(ISemanticBodyExecutionProviderFactory? provider, int runs = 1) => Run(provider, TestContext.Current.CancellationToken, runs);

    JsonElement Run(ISemanticBodyExecutionProviderFactory? provider, CancellationToken cancellationToken, int runs = 1)
    {
        var lines = new List<string>
        {
            Request(1, "initialize", new { protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "spec", version = "1" } }),
            JsonSerializer.Serialize(new { jsonrpc = "2.0", method = "notifications/initialized" }),
            Request(2, "tools/call", new { name = "open-workspace", arguments = new { } })
        };
        lines.AddRange(Enumerable.Range(3, runs).Select(id => Request(id, "tools/call", new { name = "run-specifications", arguments = new { } })));
        using var input = new StringReader(string.Join('\n', lines));
        using var output = new StringWriter();
        ScreenplayMcpServer.RunConfigured(new ScreenplayMcpRun(_root, provider, cancellationToken), input, output);
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    sealed class rule_factory : ISemanticBodyExecutionProviderFactory
    {
        internal List<rule_provider> Providers { get; } = [];
        internal SemanticApplication? Application { get; private set; }
        internal IReadOnlyDictionary<string, SemanticImplementationBody>? Bodies { get; private set; }
        internal CancellationToken Cancellation { get; private set; }

        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            Application = application;
            Bodies = bodies;
            Cancellation = cancellationToken;
            var provider = new rule_provider();
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class rule_provider : ISemanticBodyExecutionProvider
    {
        internal SemanticImplementationBody? Body { get; private set; }
        internal SemanticRuleContext? Context { get; private set; }
        internal int Disposals { get; private set; }

        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            Body = body;
            Context = context;
            return new(new SemanticBodyExecutionResult(true));
        }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
