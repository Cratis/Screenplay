// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
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
        _revision = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString()!;
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
            supplied ? ImmutableDictionary<string, string>.Empty.Add("A.cs", "return context.Value != \"no\";") : ImmutableDictionary<string, string>.Empty,
            []);
        var direct = Workspace("            file A.cs");
        var wrapped = Workspace("            implementation\n              hint \"First\"\n              hint \"Second\"\n              file A.cs");
        var changed = Workspace("            implementation\n              hint \"Second edited\"\n              hint \"First\"\n              file A.cs");
        McpAttachmentManifest.Revision(wrapped.Compilation.ImplementationRequirements).ShouldEqual(McpAttachmentManifest.Revision(direct.Compilation.ImplementationRequirements));
        McpAttachmentManifest.Revision(changed.Compilation.ImplementationRequirements).ShouldEqual(McpAttachmentManifest.Revision(direct.Compilation.ImplementationRequirements));
        wrapped.Compilation.ImplementationRequirements.Single().ContentHash.ShouldEqual(direct.Compilation.ImplementationRequirements.Single().ContentHash);
    }

    static JsonElement Content(JsonElement response) => response.GetProperty("result").GetProperty("structuredContent");
}
