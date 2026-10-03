// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_unresolved_handler_placements : given.a_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _details;
    JsonElement _resolvedDetails;
    ScreenplayWorkspace _workspace;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    import \"slice.play\"\n  feature G\n    import \"slice.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "slice.play"), "slice StateChange S\n  command C\n    handler\n      implementation\n        hint \"Keep\"\n");
        File.WriteAllText(Path.Combine(RootPath, "valid.play"), "module Other\n  feature F\n    slice StateChange S\n      command C\n        handler\n          implementation\n            hint \"Useful\"\n");
        var application = ApplicationIdentity.Create("Projects");
        var owner = SemanticAddress.ForCommand(SemanticAddress.ForSlice(application, "M", ["F"], "S"), "C");
        var id = SemanticId.Create(owner);
        var catalog = SemanticIdentityCatalog.Create(application, [], [new(owner, id, SemanticIdentityOrigin.Persisted)], []);
        var workspace = ScreenplayWorkspace.Create("Projects", Root.Read(), catalog);
        _workspace = workspace;
        var resolvedDocuments = workspace.Documents.Select(document => document.Path.Value == "application.play"
            ? WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, Encoding.UTF8.GetBytes(document.Text.Replace("  feature G\n    import \"slice.play\"\n", string.Empty, StringComparison.Ordinal)))
            : document);
        var previous = ScreenplayWorkspace.Create("Projects", [.. resolvedDocuments], catalog);
        var unresolvedId = WorkspaceImplementationInventory.Create(previous).Entries.Single(entry => entry.Owner.Equals(owner)).RequirementId;
        Initialize();
        var opened = Call("open-workspace", new { workspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(workspace)) }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var catalogRevision = workspace.IdentityCatalog.Revision.ToString();
        _first = Call("read-workspace", new { expectedRevision = revision, view = "handler-intents", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        _second = Call("read-workspace", new { expectedRevision = revision, expectedCatalogRevision = catalogRevision, view = "handler-intents", limit = 1, offset = 1 }).GetProperty("result").GetProperty("structuredContent");
        _details = Call("read-workspace", new { expectedRevision = revision, view = "handler-intent-details", requirementId = unresolvedId }).GetProperty("result");
        var resolved = _first.GetProperty("page").GetProperty("items")[0].GetProperty("requirementId").GetString();
        _resolvedDetails = Call("read-workspace", new { expectedRevision = revision, view = "handler-intent-details", requirementId = resolved }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact]
    void should_page_unresolved_documents_without_claiming_a_selected_owner_or_identity()
    {
        _first.GetProperty("unresolvedPlacementCount").GetInt32().ShouldEqual(1);
        _first.GetProperty("page").GetProperty("totalCount").GetInt32().ShouldEqual(2);
        var item = _second.GetProperty("page").GetProperty("items")[0];
        item.GetProperty("placementStatus").GetString().ShouldEqual("unresolved");
        item.GetProperty("conflictKind").GetString().ShouldEqual("UnresolvedPlacement");
        item.GetProperty("path").GetString().ShouldEqual("slice.play");
        item.TryGetProperty("owner", out _).ShouldBeFalse();
        item.TryGetProperty("ownerId", out _).ShouldBeFalse();
        item.TryGetProperty("requirementId", out _).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_unresolved_details_as_an_actionable_tool_conflict()
    {
        _details.GetProperty("isError").GetBoolean().ShouldBeTrue();
        var text = _details.GetProperty("content")[0].GetProperty("text").GetString()!;
        text.ShouldContain("UnresolvedPlacement:");
        text.ShouldContain("repair conflicting or cyclic imports");
        text.Contains("Internal", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_unprovable_persisted_identity_continuity_without_replacing_state()
    {
        var path = new McpManagedFiles(Root).PathFor(McpState.FileName, create: true);
        var bytes = McpState.Serialize(_workspace);
        McpManagedFiles.WritePrivate(path, bytes);
        var result = Call("open-workspace").GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
        result.GetProperty("content")[0].GetProperty("text").GetString()!.ShouldContain("UnresolvedPlacement:");
        File.ReadAllBytes(path).ShouldEqual(bytes);
    }

    [Fact]
    void should_keep_resolved_pending_handlers_useful_without_esm() =>
        _resolvedDetails.GetProperty("page").GetProperty("items")[0].GetString().ShouldEqual("Useful");
}
