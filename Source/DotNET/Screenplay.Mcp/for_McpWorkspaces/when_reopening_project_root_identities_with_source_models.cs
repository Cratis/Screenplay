// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_reopening_project_root_identities_with_source_models : given.a_persisted_nested_model
{
    JsonElement _opened;
    protected override string ModelFolder => "Source";

    void Because() => _opened = Result(Workspaces.Open(McpJson.Empty));

    [Fact] void should_bind_the_project_root() => Workspaces.ReadRoot().DirectoryPath.TrimEnd(Path.DirectorySeparatorChar).ShouldEqual(RootPath);
    [Fact] void should_preserve_the_applied_catalog() => _opened.GetProperty("catalogRevision").GetString().ShouldEqual(Applied.IdentityCatalog.Revision.ToString());
    [Fact] void should_preserve_the_workspace_revision() => _opened.GetProperty("revision").GetString().ShouldEqual(Applied.Revision.ToString());
    [Fact] void should_leave_identity_bytes_unchanged() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
}
