// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_a_path_with_persisted_identities : given.comparison_sources
{
    JsonElement _result;
    byte[] _identities = [];
    string _identityPath = null!;

    void Establish()
    {
        var candidate = Rename(McpWorkspaceTransport.Restore(Export)).Workspace;
        File.Move(Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "renamed.play"));
        McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        _identityPath = Path.Combine(RootPath, ".screenplay", "identities.json");
        _identities = McpState.Serialize(candidate);
        using var state = McpFileAccess.CreatePrivate(_identityPath);
        state.Write(_identities);
        Export = given.two_snapshots.Export(candidate);
    }

    void Because() => _result = Compare(new { before = new { path = RootPath }, after = new { workspaceJson = Export } });

    [Fact] void should_restore_persisted_document_and_catalog_identities() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_match_the_export_revision_exactly() => _result.GetProperty("structuredContent").GetProperty("beforeRevision").GetString().ShouldEqual(_result.GetProperty("structuredContent").GetProperty("afterRevision").GetString());
    [Fact] void should_keep_identity_matching() => _result.GetProperty("structuredContent").GetProperty("limits").GetArrayLength().ShouldEqual(4);
    [Fact] void should_find_no_changes() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
    [Fact] void should_leave_identity_bytes_unchanged() => File.ReadAllBytes(_identityPath).SequenceEqual(_identities).ShouldBeTrue();
    [Fact] void should_not_create_other_metadata() => Directory.GetFileSystemEntries(Path.Combine(RootPath, ".screenplay")).Select(Path.GetFileName).ShouldContainOnly("identities.json");
    [Fact] void should_leave_source_unchanged() => File.ReadAllText(Path.Combine(RootPath, "renamed.play")).ShouldEqual(Source);
}
