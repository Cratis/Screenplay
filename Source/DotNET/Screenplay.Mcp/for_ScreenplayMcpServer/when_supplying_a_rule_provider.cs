// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using NSubstitute;

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
    ISemanticBodyExecutionProvider _provider = null!;

    static string Request(int id, string method, object parameters) => JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });

    void Establish()
    {
        _root = SpecFixtureDirectory.Create("rule-provider");
        _provider = Substitute.For<ISemanticBodyExecutionProvider>();
        _provider.Evaluate(Arg.Any<SemanticImplementationBody>(), Arg.Any<SemanticRuleContext>()).Returns(new SemanticBodyExecutionResult(true));
    }

    void Destroy() => SpecFixtureDirectory.Delete(_root);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_resolve_inline_and_file_bodies_through_the_public_host(bool file)
    {
        WriteModel(file);
        var report = Run(_provider);
        report.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString().ShouldBeNull();
        report.GetProperty("passed").GetInt32().ShouldEqual(1);
        report.GetProperty("unsupported").GetInt32().ShouldEqual(0);
        _provider.Received(1).Evaluate(
            Arg.Is<SemanticImplementationBody>(body => body.Content == Body && body.Requirement.File != null == file),
            Arg.Is<SemanticRuleContext>(context => context.Value.Equals(SemanticValue.Text("First")) && context.Occurred == new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero) && context.CausedBy == null));
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

    void WriteModel(bool file)
    {
        var source = file ? Source.Replace("            ```csharp\n            return context.Value != null;\n            ```", "            file Rule.cs", StringComparison.Ordinal) : Source;
        File.WriteAllText(Path.Combine(_root, "orders.play"), source);
        File.WriteAllText(Path.Combine(_root, "Rule.cs"), Body);
    }

    JsonElement Run(ISemanticBodyExecutionProvider? provider)
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
        ScreenplayMcpServer.Run(_root, input, output, provider);
        using var response = JsonDocument.Parse(output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").Clone();
    }
}
