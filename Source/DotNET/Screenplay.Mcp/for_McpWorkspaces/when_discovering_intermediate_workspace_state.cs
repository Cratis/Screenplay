// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_intermediate_workspace_state : given.a_persisted_nested_model
{
    string _stateRoot = null!;
    JsonElement _opened;

    void Establish()
    {
        _stateRoot = Path.Combine(RootPath, "Source");
        Directory.CreateDirectory(_stateRoot);
        Directory.Move(Path.Combine(RootPath, ModelFolder), Path.Combine(_stateRoot, ModelFolder));
        Directory.Move(Path.Combine(RootPath, ".screenplay"), Path.Combine(_stateRoot, ".screenplay"));
    }

    void Because() => _opened = Result(Workspaces.Open(McpJson.Empty));

    [Fact] void should_bind_the_intermediate_state_root() => Workspaces.ReadRoot().DirectoryPath.ShouldEqual(_stateRoot);
    [Fact] void should_preserve_the_applied_catalog() => _opened.GetProperty("catalogRevision").GetString().ShouldEqual(Applied.IdentityCatalog.Revision.ToString());
    [Fact] void should_preserve_the_workspace_revision() => _opened.GetProperty("revision").GetString().ShouldEqual(Applied.Revision.ToString());
    [Fact] void should_not_create_state_at_the_offered_root() => Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
}
