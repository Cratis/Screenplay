// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_associating_ordered_production_generations
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_resolve_one_logical_event_and_preserve_order_and_source_handles(bool reverseFiles, bool reverseGenerations)
    {
        var generations = new[] { "      event Recorded\n", "      event Recorded generation 2\n" };
        var documents = new[]
        {
            Document("commands.play", "module M\n  feature F\n    slice StateChange Commands\n      command C\n        produces History.Recorded\n        produces Send\n        produces History.Recorded\n      operation Send\n        uses Mailer\n        execute\n          file Send.cs\n"),
            Document("history.play", "system Mailer\nmodule M\n  feature F\n    slice StateChange History\n" + string.Concat(reverseGenerations ? generations.Reverse() : generations)),
            Document("other.play", "module M\n  feature F\n    slice StateChange Other\n      event Recorded\n")
        };
        var inventory = Inventory(reverseFiles ? documents.Reverse() : documents);
        var productions = Rows(inventory);
        productions.Select(row => row.GetProperty("targetKind").GetString()).SequenceEqual(["Event", "Operation", "Event"]).ShouldBeTrue();
        productions.Select(row => row.GetProperty("order").GetInt32()).SequenceEqual([0, 1, 2]).ShouldBeTrue();
        foreach (var row in productions)
        {
            row.GetProperty("candidates").GetArrayLength().ShouldEqual(1);
            row.GetProperty("location").GetProperty("path").GetString().ShouldEqual("commands.play");
            row.GetProperty("handle").GetProperty("documentId").GetString().ShouldEqual(documents[0].Id.ToString());
            row.GetProperty("handle").GetProperty("path").GetString().ShouldEqual($"/modules/0/features/0/slices/0/commands/0/produces/{row.GetProperty("order").GetInt32()}");
            row.GetProperty("executionAvailable").GetBoolean().ShouldBeFalse();
        }
        productions[0].GetProperty("candidates")[0].GetProperty("address").GetString().ShouldEqual("M.F.History.Recorded");
        var operation = inventory.Entries.Single(entry => entry.Node is Syntax.OperationSyntax);
        var summary = JsonSerializer.SerializeToElement(inventory.Summary(operation), Options);
        summary.GetProperty("execute").GetProperty("sourceHandle").GetProperty("documentId").GetString().ShouldEqual(documents[0].Id.ToString());
        summary.GetProperty("execute").GetProperty("sourceHandle").GetProperty("path").GetString().ShouldEqual("/modules/0/features/0/slices/0/operations/0/execute/file");
    }

    [Theory]
    [InlineData("      event Recorded\n      event Recorded\n")]
    [InlineData("      event Recorded generation 2\n      event Recorded generation 2\n")]
    [InlineData("      event Recorded\n      operation Recorded\n        uses Mailer\n")]
    public void should_not_hide_generation_or_kind_collisions(string declarations)
    {
        var inventory = Inventory([Document("model.play", "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n" + declarations + "      command C\n        produces Recorded\n")]);
        var production = Rows(inventory).Single();
        production.GetProperty("targetKind").GetString().ShouldEqual("Ambiguous");
        production.GetProperty("candidates").GetArrayLength().ShouldEqual(2);
    }

    [Fact]
    public void should_not_select_production_kinds_without_a_unique_physical_command_owner()
    {
        var inventory = Inventory([Document("model.play", "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n      operation Send\n        uses Mailer\n      command C\n        produces Send\n      command C\n        produces Send\n")]);
        var productions = Rows(inventory);
        productions.ShouldNotBeEmpty();
        productions.All(row => row.GetProperty("targetKind").GetString() == "Unresolved").ShouldBeTrue();
    }

    static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    static JsonElement[] Rows(McpOperationInventory inventory) => [.. inventory.Productions().Select(row => JsonSerializer.SerializeToElement(row, Options))];

    static McpOperationInventory Inventory(IEnumerable<WorkspaceDocument> documents)
    {
        var workspace = ScreenplayWorkspace.Create("Example", [.. documents], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Example")));

        return new(workspace, WorkspaceSyntaxIndex.Create(workspace));
    }

    static WorkspaceDocument Document(string path, string source) => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));
}
