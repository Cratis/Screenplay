// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_the_client_offers_a_project_without_a_model : given.a_dynamic_connection
{
    JsonElement _opened;
    JsonElement _state;

    void Establish() => Initialize(true, RootsAnswer(EmptyPath));

    void Because()
    {
        _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        _state = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_bind_the_offered_project() => Tools.ClientDerivedRootPath.ShouldEqual(EmptyPath);
    [Fact] void should_open_an_empty_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_report_absent_state() => _state.GetProperty("exists").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_create_a_model_folder() => Directory.EnumerateFileSystemEntries(EmptyPath).ShouldBeEmpty();
}
