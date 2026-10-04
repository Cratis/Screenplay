// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_resolving_operation_specification_dependencies
{
    const string Operation = "      operation Send\n        uses Mailer\n";
    const string Event = "      event Send\n";
    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("sibling", "Send", "ambiguous", 2)]
    [InlineData("shadow", "Send", "wrongKind", 1)]
    [InlineData("unique", "Send", "resolved", 1)]
    [InlineData("collision", "Send", "ambiguous", 2)]
    [InlineData("sibling", "A.Send", "resolved", 1)]
    [InlineData("inline", "Send", "resolved", 1)]
    [InlineData("sibling", "Opaque.Send", "unresolved", 0)]
    public void should_resolve_all_step_kinds_before_requiring_an_operation(string scenario, string reference, string expected, int count)
    {
        var declarations = scenario switch
        {
            "sibling" => "    slice StateChange A\n" + Operation + "    slice StateChange B\n" + Event,
            "shadow" => "    slice StateChange A\n" + Operation,
            _ => string.Empty
        };
        var local = scenario switch
        {
            "shadow" => Event,
            "unique" => Operation,
            "collision" => Event + Operation,
            _ => string.Empty
        };
        var production = scenario == "inline" ? "        produces operation Send\n          uses Mailer\n" : string.Empty;
        var source = "system Mailer\nimport Opaque.Send\nmodule M\n  feature F\n" + declarations + "    slice StateChange C\n" + local +
            "      command Ask\n" + production + "      specification T\n        given operation " + reference + " fails\n        when Ask\n        then operation " + reference + "\n        then compensated " + reference + "\n";
        var snapshot = new McpSnapshot([Document("model.play", source)]);
        var arguments = JsonSerializer.SerializeToElement(new { address = "M.F.C.T", kind = "Specification", direction = "outgoing" });
        var result = JsonSerializer.SerializeToElement(McpDependencyQueries.Read(snapshot, arguments), Options);
        var rows = result.GetProperty("page").GetProperty("items").EnumerateArray()
            .Where(row => row.GetProperty("reference").GetProperty("kinds").EnumerateArray().Any(kind => kind.GetString() == "Operation")).ToArray();
        rows.Length.ShouldEqual(3);
        rows.Select(row => row.GetProperty("reference").GetProperty("role").GetString()).Order(StringComparer.Ordinal)
            .SequenceEqual(["givenOperationFailure", "thenCompensated", "thenOperation"]).ShouldBeTrue();
        foreach (var row in rows)
        {
            row.GetProperty("resolution").GetString().ShouldEqual(expected);
            var targets = row.GetProperty("targets").EnumerateArray().ToArray();
            targets.Length.ShouldEqual(count);
            if (expected == "ambiguous") targets.Select(target => target.GetProperty("kind").GetString()).Order(StringComparer.Ordinal).SequenceEqual(["Event", "Operation"]).ShouldBeTrue();
            if (expected == "wrongKind") targets.Single().GetProperty("kind").GetString().ShouldEqual("Event");
            if (expected == "resolved") targets.Single().GetProperty("kind").GetString().ShouldEqual("Operation");
            row.TryGetProperty("semanticId", out _).ShouldBeFalse();
        }
        var specification = snapshot.Index.Find("M.F.C.T", "Specification").Single();
        snapshot.Index.Readiness.ExecutionReadiness(specification.Syntax).ShouldContain("ESM v9 (PLAY0268)");
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
