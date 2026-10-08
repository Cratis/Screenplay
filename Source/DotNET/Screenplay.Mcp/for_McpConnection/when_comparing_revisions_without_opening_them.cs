// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_comparing_revisions_without_opening_them : given.a_connection
{
    JsonElement _result;
    JsonElement _opened;
    JsonElement _current;
    byte[] _original = [];

    void Establish()
    {
        Initialize();
        _original = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
    }

    void Because()
    {
        var baseline = Workspace();
        var candidate = Rename(baseline).Workspace;
        _result = Call("semantic-diff", new
        {
            beforeWorkspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(baseline)),
            afterWorkspaceJson = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(candidate)),
            limit = 1
        }).GetProperty("result");
        _current = Call("read-workspace", new { expectedRevision = _opened.GetProperty("revision").GetString() }).GetProperty("result");
    }

    [Fact] void should_work_without_mcp_apps() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_return_a_bounded_structural_page() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_keep_the_active_workspace() => _current.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_leave_source_untouched() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_original).ShouldBeTrue();
    [Fact] void should_not_persist_imported_identities() => File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
}
