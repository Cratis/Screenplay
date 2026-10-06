// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_workspace_diagnostics : given.a_connection
{
    JsonElement _result;
    string _revision;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateChange Add
                  event Added
                    value String
            """);
        File.WriteAllText(Path.Combine(RootPath, "other.play"), """
            module Other
              feature F
                slice StateChange Broken
                  event OtherEvent
                    value UnknownType
            """);
        Initialize();
        _revision = Call("open-workspace").GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString()!;
    }

    void Because() => _result = Call("read-workspace", new { expectedRevision = _revision, view = "diagnostics", scope = "Projects.Registration" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_return_a_scoped_workspace_page() => _result.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    [Fact] void should_report_the_scoped_diagnostic_summary() => _result.GetProperty("summary").GetProperty("total").GetInt32().ShouldEqual(0);
    [Fact] void should_count_the_feature_slice_and_event() => _result.GetProperty("declarationCount").GetInt32().ShouldEqual(3);
    [Fact] void should_keep_the_workspace_envelope() => _result.GetProperty("workspace").GetProperty("revision").GetString().ShouldEqual(_revision);
}
