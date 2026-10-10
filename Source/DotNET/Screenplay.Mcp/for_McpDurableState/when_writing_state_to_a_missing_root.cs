// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_writing_state_to_a_missing_root : for_McpConnection.given.a_connection
{
    McpManagedFiles _files = null!;
    byte[] _state = [];

    void Establish()
    {
        Root = new(Path.Combine(RootPath, "Screenplay"));
        _files = new(Root);
        _state = McpState.Serialize(ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Projects"), "Projects"));
    }

    void Because() => McpManagedFiles.WritePrivate(_files.PathFor(McpState.FileName, create: true), _state);

    [Fact] void should_create_the_root() => Root.Exists.ShouldBeTrue();
    [Fact] void should_persist_the_identity_state() => _files.Read(McpState.FileName)!.ShouldContainOnly(_state);
    [Fact] void should_reopen_the_empty_workspace() => McpState.Deserialize(_files.Read(McpState.FileName)!).Open(Root).Documents.ShouldBeEmpty();
}
