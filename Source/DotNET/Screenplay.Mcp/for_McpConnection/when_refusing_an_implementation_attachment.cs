// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_refusing_an_implementation_attachment : given.a_connection
{
    JsonElement _diagnostics;
    JsonElement _requirements;
    JsonElement _sourceMap;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    handler
                      file ../refused.cs
            """);
        Initialize();
        var revision = Call("open-workspace").GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _diagnostics = Content("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" });
        _requirements = Content("read-workspace", new { expectedRevision = revision, view = "implementation-requirements" });
        _sourceMap = Content("read-workspace", new { expectedRevision = revision, view = "source-map" });
    }

    [Fact] void should_preserve_the_refusal_warning() => _diagnostics.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0430").GetProperty("severity").GetString().ShouldEqual("Warning");
    [Fact]
    void should_keep_warnings_discoverable_even_when_the_source_map_is_unavailable()
    {
        _sourceMap.GetProperty("available").GetBoolean().ShouldBeFalse();
        _sourceMap.GetProperty("executableDiagnosticsCount").GetInt32().ShouldEqual(_diagnostics.GetProperty("page").GetProperty("totalCount").GetInt32());
        _sourceMap.GetProperty("executableDiagnosticsView").GetString().ShouldEqual("executable-diagnostics");
    }
    [Fact] void should_keep_the_requirement_unresolved_and_unhashed()
    {
        var requirement = _requirements.GetProperty("page").GetProperty("items")[0];
        requirement.GetProperty("attachmentResolution").GetString().ShouldEqual("UnresolvedFile");
        requirement.GetProperty("contentHash").GetString().ShouldBeEmpty();
    }

    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
}
