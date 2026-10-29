// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp.for_ScreenplayMcpServer;

public class when_supplying_a_code_validation_provider : Specification
{
    const string Body = "yield break;";
    const string Source = """
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
              specification Valid
                given clock "2030-01-02T03:04:05Z"
                when Place
                  name = "First"
                then Placed
                  for "First"
        """;

    string _root = null!;
    validation_factory _factory = null!;

    void Establish()
    {
        _root = SpecFixtureDirectory.Create("validation-provider");
        _factory = new();
    }

    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_resolve_inline_and_file_validations_through_the_public_host(bool file)
    {
        WriteModel(file);
        Run(_factory).GetProperty("passed").GetInt32().ShouldEqual(1);
        var provider = _factory.Providers.Single();
        provider.Body!.Content.ShouldEqual(Body);
        provider.Body.Requirement.Role.ShouldEqual(SemanticImplementationRole.CommandValidation);
        (provider.Body.Requirement.File is not null).ShouldEqual(file);
        provider.Context!.Artifact.ShouldBeOfExactType<SemanticCompositeValue>();
        provider.Context.Value.ShouldEqual(provider.Context.Artifact);
        provider.Context.Property.ShouldEqual(string.Empty);
        provider.Context.Occurred.ShouldEqual(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        provider.Disposals.ShouldEqual(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_preserve_the_default_host_refusal(bool file)
    {
        WriteModel(file);
        var report = Run(null);
        report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
        report.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString().ShouldEqual("Command 'Place' code validation 0 has an opaque validation predicate and requires a target provider.");
    }

    [Fact]
    void should_create_and_dispose_isolated_providers()
    {
        WriteModel(false);
        Run(_factory, 2).GetProperty("passed").GetInt32().ShouldEqual(1);
        _factory.Providers.Count.ShouldEqual(2);
        _factory.Providers.TrueForAll(provider => provider.Disposals == 1).ShouldBeTrue();
    }

    void WriteModel(bool file)
    {
        File.WriteAllText(Path.Combine(_root, "orders.play"), file ? Source.Replace("          ```csharp\n          yield break;\n          ```", "          file Validate.cs", StringComparison.Ordinal) : Source);
        File.WriteAllText(Path.Combine(_root, "Validate.cs"), Body);
    }

    JsonElement Run(ISemanticBodyExecutionProviderFactory? provider, int runs = 1)
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
        ScreenplayMcpServer.RunConfigured(new ScreenplayMcpRun(_root, provider, TestContext.Current.CancellationToken), input, output);
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    sealed class validation_factory : ISemanticBodyExecutionProviderFactory
    {
        internal List<validation_provider> Providers { get; } = [];

        public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
        {
            var provider = new validation_provider();
            Providers.Add(provider);
            return new(provider);
        }
    }

    sealed class validation_provider : ISemanticBodyExecutionProvider
    {
        internal SemanticImplementationBody? Body { get; private set; }
        internal SemanticRuleContext? Context { get; private set; }
        internal int Disposals { get; private set; }

        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) => throw new InvalidOperationException();

        public ValueTask<SemanticValidationBodyResult> ValidateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
        {
            Body = body;
            Context = context;
            return new(SemanticValidationBodyResult.Valid());
        }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
}
