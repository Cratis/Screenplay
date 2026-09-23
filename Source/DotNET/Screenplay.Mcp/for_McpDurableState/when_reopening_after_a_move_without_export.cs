// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_reopening_after_a_move_without_export : given.a_durable_workspace
{
    JsonElement _opened;
    McpState _persisted = null!;

    void Because()
    {
        new McpDisk(Root).Apply(Proposal).Success.ShouldBeTrue();
        _opened = Result(new McpWorkspaces(new McpRoot(RootPath)).Open(McpJson.Empty));
        _persisted = McpState.Deserialize(Files.Read(McpState.FileName));
    }

    [Fact] void should_reopen_without_an_export() => _opened.GetProperty("revision").GetString().ShouldEqual(Proposal.Workspace.Revision.ToString());
    [Fact] void should_preserve_the_document_identity() => _persisted.Mappings.Single().Id.ShouldEqual(Original.Documents.Single().Id);
    [Fact] void should_preserve_semantic_identities() => _persisted.Catalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(Original.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
    [Fact] void should_preserve_event_identities() => _persisted.Catalog.EventContracts.Select(assignment => assignment.Id).ShouldContainOnly(Original.IdentityCatalog.EventContracts.Select(assignment => assignment.Id));
    [Fact] void should_persist_the_moved_path() => _persisted.Mappings.Single().Path.Value.ShouldEqual("renamed.play");
    [Fact] void should_not_duplicate_source_text() => System.Text.Encoding.UTF8.GetString(Files.Read(McpState.FileName)).Contains("Registers a new project", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_remove_the_pending_marker_only_after_verification() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
