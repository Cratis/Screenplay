// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_discovering_and_enforcing_typed_batch_operations : given.an_authoring_connection
{
    JsonElement _tools;
    JsonElement _strict;
    JsonElement _ast;
    JsonElement _syntax;
    readonly List<JsonElement> _rejections = [];
    byte[] _original = [];

    void Establish()
    {
        _original = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        using var listed = JsonDocument.Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}"""));
        _tools = listed.RootElement.GetProperty("result").GetProperty("tools").Clone();
        _strict = Schema("propose").GetProperty("properties").GetProperty("operations").GetProperty("items").GetProperty("oneOf");
        _ast = Schema("propose-ast").GetProperty("properties").GetProperty("operations").GetProperty("items").GetProperty("oneOf");
        _syntax = Result("syntax-schema", new { kind = "CommandSyntax" });
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var command = Node("CommandSyntax", revision);
        var handle = command.GetProperty("handle");
        foreach (var operation in new object[]
        {
            new { operation = "replace-document", documentId = handle.GetProperty("documentId").GetString(), bytesBase64 = Convert.ToBase64String(_original), unexpected = true },
            new { operation = "invented-operation" },
            new { operation = "replace-document", documentId = handle.GetProperty("documentId").GetString(), bytesBase64 = 42 }
        })
        {
            _rejections.Add(Call("propose", new { expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), operations = new[] { operation } }));
        }

        var invalidNode = JsonNode.Parse(command.GetProperty("node").GetRawText());
        invalidNode["inventedMember"] = true;
        foreach (var operation in new object[]
        {
            new { operation = "replace", target = handle, node = command.GetProperty("node"), unexpected = true },
            new { operation = "invented-operation", target = handle },
            new { operation = "replace", target = handle, node = invalidNode }
        })
        {
            _rejections.Add(Call("propose-ast", new { expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), formatting = "CanonicalizeTouchedDocuments", operations = new[] { operation } }));
        }
    }

    [Fact]
    void should_advertise_all_public_tools() => _tools.EnumerateArray().Select(tool => tool.GetProperty("name").GetString()).ShouldContainOnly(
        ["describe-application", "find-declaration", "search-declarations", "declaration-details", "dependencies", "find-references",
        "find-fixtures", "find-assertion-gaps", "merged-document", "read-document", "diagnostics", "recommend-layout", "syntax-schema",
        "open-workspace", "workspace-state", "recover-workspace", "propose-rename", "read-workspace", "read-ast", "propose", "propose-repair", "propose-ast",
        "expand-layout", "read-proposal", "export-workspace", "discard-proposal", "apply"]);
    [Fact] void should_advertise_all_six_strict_document_operation_shapes() => Names(_strict).ShouldContainOnly("update-slice-description", "move-document", "rename-document-key", "add-document", "replace-document", "remove-document");
    [Fact] void should_advertise_all_four_typed_ast_operation_shapes() => Names(_ast).ShouldContainOnly("add", "replace", "remove", "move");
    [Fact] void should_forbid_unknown_members_in_every_operation_shape() => _strict.EnumerateArray().Concat(_ast.EnumerateArray()).All(shape => !shape.GetProperty("additionalProperties").GetBoolean()).ShouldBeTrue();
    [Fact] void should_advertise_object_handles_for_replacement_targets() => _ast.EnumerateArray().Single(shape => NamesOf(shape) == "replace").GetProperty("properties").GetProperty("target").GetProperty("type").GetString().ShouldEqual("object");
    [Fact] void should_advertise_typed_object_nodes_not_opaque_json_strings() => _ast.EnumerateArray().Single(shape => NamesOf(shape) == "replace").GetProperty("properties").GetProperty("node").GetProperty("type").GetString().ShouldEqual("object");
    [Fact] void should_supply_a_closed_command_syntax_schema() => _syntax.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    [Fact] void should_check_every_invalid_request() => _rejections.Count.ShouldEqual(6);
    [Fact] void should_reject_all_invalid_and_unknown_fields_or_operations() => _rejections.TrueForAll(response => response.TryGetProperty("error", out _) || response.GetProperty("result").GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_not_write_on_any_rejection() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_original).ShouldBeTrue();

    JsonElement Schema(string name) => _tools.EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == name).GetProperty("inputSchema");

    static IEnumerable<string> Names(JsonElement alternatives) => alternatives.EnumerateArray().Select(NamesOf);

    static string NamesOf(JsonElement shape) => shape.GetProperty("properties").GetProperty("operation").GetProperty("enum")[0].GetString()!;
}
