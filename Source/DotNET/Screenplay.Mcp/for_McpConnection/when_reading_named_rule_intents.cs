// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_named_rule_intents : given.a_connection
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule Check\n";
    JsonElement _page;
    string _revision = null!;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "model.play"), Prefix + "            implementation\n              hint \"Pending\"\n          label rule Check\n            file A.cs\n          label rule Check\n            implementation\n              hint \"Attached\"\n              file B.cs");
        Initialize();
        var opened = Content(Call("open-workspace", new { applicationName = "Projects" }));
        _revision = opened.GetProperty("revision").GetString()!;
        _page = Content(Call("read-workspace", new { expectedRevision = _revision, view = "named-rule-intents" }));
    }

    [Fact]
    void should_expose_command_only_coverage_and_pending_without_a_requirement_id()
    {
        _page.GetProperty("coverage").GetString().ShouldEqual("CommandNamedRule");
        var items = _page.GetProperty("page").GetProperty("items");
        items.GetArrayLength().ShouldEqual(3);
        items[0].GetProperty("requirementId").ValueKind.ShouldEqual(JsonValueKind.Null);
        items[0].GetProperty("executionEvidence").GetBoolean().ShouldBeFalse();
        items[1].GetProperty("requirementId").GetString().ShouldNotEqual(items[2].GetProperty("requirementId").GetString());
        items[2].GetProperty("member").GetString().ShouldEqual("label/Check#1");
    }

    [Fact]
    void should_read_pending_by_handle_and_attached_duplicates_by_their_distinct_ids()
    {
        var items = _page.GetProperty("page").GetProperty("items");
        var pending = Content(Call("read-workspace", new { expectedRevision = _revision, view = "named-rule-intent-details", subject = items[0].GetProperty("handle") }));
        pending.GetProperty("page").GetProperty("items")[0].GetString().ShouldEqual("Pending");
        foreach (var entry in items.EnumerateArray().Skip(1))
        {
            var details = Content(Call("read-workspace", new { expectedRevision = _revision, view = "named-rule-intent-details", requirementId = entry.GetProperty("requirementId").GetString() }));
            details.GetProperty("rule").GetProperty("member").GetString().ShouldEqual(entry.GetProperty("member").GetString());
        }
    }

    [Fact]
    void should_return_canonical_owner_addresses_that_round_trip_through_the_reader()
    {
        var items = _page.GetProperty("page").GetProperty("items");
        var expected = SemanticAddress.ForCommand(SemanticAddress.ForSlice(ApplicationIdentity.Create("Projects"), "M", ["F"], "S"), "C");
        foreach (var item in items.EnumerateArray())
        {
            var owner = item.GetProperty("owner");
            owner.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ShouldEqual(["kind", "parts"]);
            McpSemanticAddresses.Read(owner).ShouldEqual(expected);
        }
    }

    [Fact]
    void should_accept_a_returned_owner_address_in_a_typed_identity_migration()
    {
        // Explicit identity migrations apply to assigned owners, not provisional bootstrap addresses.
        // Persist the existing command identity; the pending rule itself remains ID-free.
        var pending = _page.GetProperty("page").GetProperty("items")[0];
        var assignment = new SemanticIdentityAssignment(McpSemanticAddresses.Read(pending.GetProperty("owner")), SemanticId.Parse(pending.GetProperty("ownerId").GetString()!), SemanticIdentityOrigin.Persisted);
        var propertyAddress = SemanticAddress.ForProperty(assignment.Address, "label");
        var property = new SemanticIdentityAssignment(propertyAddress, SemanticId.Create(propertyAddress), SemanticIdentityOrigin.Persisted);
        var catalog = SemanticIdentityCatalog.Create(ApplicationIdentity.Create("Projects"), [], [assignment, property], []);
        var workspace = ScreenplayWorkspace.Create("Projects", Root.Read(), catalog);
        var opened = Content(Call("open-workspace", new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(workspace)) }));
        var revision = opened.GetProperty("revision").GetString();
        var page = Content(Call("read-workspace", new { expectedRevision = revision, view = "named-rule-intents" }));
        var persisted = page.GetProperty("page").GetProperty("items")[0];
        persisted.GetProperty("isProvisional").GetBoolean().ShouldBeFalse();
        persisted.GetProperty("requirementId").ValueKind.ShouldEqual(JsonValueKind.Null);
        var owner = persisted.GetProperty("owner");
        var renamed = JsonNode.Parse(owner.GetRawText())!;
        renamed["parts"]!.AsArray()[^1]!["key"] = "Renamed";
        var ast = Content(Call("read-ast", new { expectedRevision = revision, kind = "CommandSyntax", includeContent = true }));
        var command = ast.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("node").GetProperty("name").GetString() == "C");
        var node = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        node["name"] = "Renamed";
        var proposed = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            operations = new[] { new { operation = "replace", target = command.GetProperty("handle"), node } },
            semanticRenames = new[]
            {
                new { previousAddress = (object)owner, currentAddress = (object)renamed },
                new { previousAddress = (object)JsonSerializer.SerializeToElement(McpSemanticAddresses.Describe(propertyAddress), McpJson.Options), currentAddress = (object)JsonSerializer.SerializeToElement(McpSemanticAddresses.Describe(SemanticAddress.ForProperty(SemanticAddress.ForCommand(SemanticAddress.ForSlice(catalog.Application, "M", ["F"], "S"), "Renamed"), "label")), McpJson.Options) }
            }
        }).GetProperty("result");
        Assert.False(proposed.TryGetProperty("isError", out var error) && error.GetBoolean(), proposed.GetRawText());
        proposed.GetProperty("structuredContent").GetProperty("proposalId").GetString().ShouldNotBeNull();
    }

    [Fact]
    void should_refuse_stale_catalog_continuations()
    {
        var result = Call("read-workspace", new { expectedRevision = _revision, view = "named-rule-intents", offset = 1, expectedCatalogRevision = new string('0', 64) }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_attachment_manifest_revision_when_wrapping_and_editing_hints(bool supplied)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A"));
        ScreenplayWorkspace Workspace(string body) => ScreenplayWorkspace.Create(
            catalog.Application,
            "A",
            [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Prefix + body))],
            catalog,
            supplied ? ImmutableDictionary<string, string>.Empty.Add("A.cs", "return context.Value != \"no\";") : [],
            []);
        var direct = Workspace("            file A.cs");
        var wrapped = Workspace("            implementation\n              hint \"First\"\n              hint \"Second\"\n              file A.cs");
        var changed = Workspace("            implementation\n              hint \"Second edited\"\n              hint \"First\"\n              file A.cs");
        McpAttachmentManifest.Revision(wrapped.Compilation.ImplementationRequirements).ShouldEqual(McpAttachmentManifest.Revision(direct.Compilation.ImplementationRequirements));
        McpAttachmentManifest.Revision(changed.Compilation.ImplementationRequirements).ShouldEqual(McpAttachmentManifest.Revision(direct.Compilation.ImplementationRequirements));
        wrapped.Compilation.ImplementationRequirements.Single().ContentHash.ShouldEqual(direct.Compilation.ImplementationRequirements.Single().ContentHash);
    }

    [Theory]
    [InlineData("DeclarativeValidateSyntax", false)]
    [InlineData("CommandSyntax", false)]
    [InlineData("ApplicationSyntax", false)]
    [InlineData("document", false)]
    [InlineData("DeclarativeValidateSyntax", true)]
    [InlineData("CommandSyntax", true)]
    [InlineData("ApplicationSyntax", true)]
    [InlineData("document", true)]
    void should_fail_closed_for_transport_decoded_renames_and_sibling_changes(string scope, bool deleteSibling)
    {
        File.WriteAllText(Path.Combine(RootPath, "model.play"), Prefix + "            implementation\n              hint \"Keep\"" + (deleteSibling ? "\n          label not empty" : ""));
        var opened = Content(Call("open-workspace", new { applicationName = "Projects" }));
        var revision = opened.GetProperty("revision").GetString();
        var kind = scope == "document" ? "ApplicationSyntax" : scope;
        var ast = Content(Call("read-ast", new { expectedRevision = revision, kind, includeContent = true }));
        var documentId = _page.GetProperty("page").GetProperty("items")[0].GetProperty("handle").GetProperty("documentId").GetString();
        var target = ast.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("handle").GetProperty("documentId").GetString() == documentId);
        var parsed = new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + (deleteSibling ? "" : "          label not empty")).Value!;
        SyntaxNode replacement = kind switch
        {
            "CommandSyntax" => parsed.Modules.Single().Features.Single().Slices.Single().Commands.Single(),
            "DeclarativeValidateSyntax" => parsed.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single(),
            _ => parsed
        };
        var node = SyntaxJson.Serialize(replacement);
        var result = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            operations = scope == "document" ? Array.Empty<object>() : [new { operation = "replace", target = target.GetProperty("handle"), node }],
            documents = scope == "document" ? new object[] { new { operation = "replace-document", documentId = target.GetProperty("handle").GetProperty("documentId"), node } } : []
        }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
        result.GetProperty("structuredContent").GetProperty("conflicts")[0].GetProperty("message").GetString()!.Contains("pending", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_accept_only_a_validated_original_rule_removal_before_a_transport_ancestor_replacement(bool document)
    {
        var revision = _revision;
        var pending = _page.GetProperty("page").GetProperty("items")[0];
        var documentId = pending.GetProperty("handle").GetProperty("documentId").GetString();
        var root = Content(Call("read-ast", new { expectedRevision = revision, kind = "ApplicationSyntax", includeContent = true })).GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("handle").GetProperty("documentId").GetString() == documentId);
        var opened = Content(Call("read-workspace", new { expectedRevision = revision })).GetProperty("workspace");
        var node = SyntaxJson.Serialize(new ScreenplayCompiler().Parse(Prefix.Replace("rule Check", "rule Renamed", StringComparison.Ordinal) + "          label not empty").Value!);
        var result = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            operations = document ? new object[] { new { operation = "remove", target = pending.GetProperty("handle") } } :
                [new { operation = "remove", target = pending.GetProperty("handle") }, new { operation = "replace", target = root.GetProperty("handle"), node }],
            documents = document ? new object[] { new { operation = "replace-document", documentId = root.GetProperty("handle").GetProperty("documentId"), node } } : []
        }).GetProperty("result");
        Assert.False(result.TryGetProperty("isError", out var error) && error.GetBoolean(), result.GetRawText());
        result.GetProperty("structuredContent").GetProperty("proposalId").GetString().ShouldNotBeNull();
    }

    static JsonElement Content(JsonElement response) => response.GetProperty("result").GetProperty("structuredContent");
}
