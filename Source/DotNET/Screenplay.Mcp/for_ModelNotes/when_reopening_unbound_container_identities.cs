// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_reopening_unbound_container_identities : given.an_unbound_persisted_notes_workspace
{
    JsonElement _repeated;
    Exception _import = null!;
    byte[]? _after;

    void Because()
    {
        _repeated = Result(Workspaces.Open(McpJson.Empty));
        _after = Files.Read(McpState.FileName);
        _import = Catch.Exception(() => Workspaces.Open(Arguments(new { workspaceJson = System.Text.Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(Split)) })));
    }

    [Fact] void should_preserve_disk_state_on_open() => McpManagedFiles.Equal(_after, EarlierState).ShouldBeTrue();
    [Fact] void should_materialize_only_containers() => Split.IdentityCatalog.Semantics.Length.ShouldEqual(3);
    [Fact] void should_repeat_catalog_revision() => _repeated.GetProperty("catalogRevision").GetString().ShouldEqual(Opened.GetProperty("catalogRevision").GetString());
    [Fact] void should_repeat_workspace_revision() => _repeated.GetProperty("revision").GetString().ShouldEqual(Opened.GetProperty("revision").GetString());
    [Fact] void should_still_refuse_a_supplied_workspace_import() => _import.Message.ShouldContain("IdentityImportConflict");
}
