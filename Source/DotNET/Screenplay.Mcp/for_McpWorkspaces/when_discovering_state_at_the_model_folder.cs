// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_state_at_the_model_folder : given.a_persisted_nested_model
{
    string _modelRoot = null!;
    JsonElement _opened;

    void Establish()
    {
        _modelRoot = Path.Combine(RootPath, ModelFolder);
        var document = Applied.Documents.Single();
        var state = new McpState(Applied.ApplicationName, Applied.IdentityCatalog, [WorkspaceDocument.Create(document.Id, document.StableKey, PortablePlayPath.Parse("application.play"), [])]);
        var nestedFiles = new McpManagedFiles(new McpRoot(_modelRoot));
        McpManagedFiles.WritePrivate(nestedFiles.PathFor(McpState.FileName, create: true), state.Serialize());
        File.Delete(Files.PathFor(McpState.FileName));
    }

    void Because() => _opened = Result(Workspaces.Open(McpJson.Empty));

    [Fact] void should_bind_the_discovered_model_folder() => Workspaces.ReadRoot().DirectoryPath.ShouldEqual(_modelRoot);
    [Fact] void should_reopen_its_persisted_catalog() => _opened.GetProperty("catalogRevision").GetString().ShouldEqual(Applied.IdentityCatalog.Revision.ToString());
    [Fact] void should_not_report_a_competing_state_root() => _opened.TryGetProperty("rootBindingConflict", out _).ShouldBeFalse();
}
