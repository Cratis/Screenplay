// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_reopening_project_root_identities : given.a_persisted_nested_model
{
    ScreenplayWorkspace _reopened = null!;

    void Because() => _reopened = McpWorkspaceTransport.Restore(Result(Workspaces.Open(JsonSerializer.SerializeToElement(new { includeContent = true }))).GetProperty("workspaceJson").GetString()!);

    [Fact] void should_bind_the_project_root() => Workspaces.ReadRoot().DirectoryPath.TrimEnd(Path.DirectorySeparatorChar).ShouldEqual(RootPath);
    [Fact] void should_preserve_the_applied_catalog() => _reopened.IdentityCatalog.Revision.ShouldEqual(Applied.IdentityCatalog.Revision);
    [Fact] void should_preserve_the_applied_semantic_identities() => _reopened.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(Applied.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
    [Fact] void should_preserve_the_applied_event_identities() => _reopened.IdentityCatalog.EventContracts.Select(assignment => assignment.Id).ShouldContainOnly(Applied.IdentityCatalog.EventContracts.Select(assignment => assignment.Id));
    [Fact] void should_preserve_the_applied_document_identity() => _reopened.Documents.Single().Id.ShouldEqual(Applied.Documents.Single().Id);
    [Fact] void should_preserve_the_root_relative_model_path() => _reopened.Documents.Single().Path.Value.ShouldEqual("Models/application.play");
    [Fact] void should_leave_identity_bytes_unchanged() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
    [Fact] void should_not_migrate_state_to_the_model_folder() => Directory.Exists(Path.Combine(RootPath, "Models", ".screenplay")).ShouldBeFalse();
}
