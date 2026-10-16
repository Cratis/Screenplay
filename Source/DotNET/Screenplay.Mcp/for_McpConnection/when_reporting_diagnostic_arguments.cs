// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_diagnostic_arguments : given.a_connection
{
    JsonElement _diagnostic;
    JsonElement _introduced;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "example invalid");
        Initialize();
    }

    void Because()
    {
        _diagnostic = Call("diagnostics").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")
            .EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0518");
        var workspace = Workspace();
        var before = ScreenplayWorkspace.CreateEmpty(workspace.IdentityCatalog.Application, workspace.ApplicationName);
        _introduced = JsonSerializer.SerializeToElement(McpIntroducedErrors.Between(before, workspace), McpJson.Options)
            .EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0518");
    }

    [Fact] void should_expose_the_declared_subject() => _diagnostic.GetProperty("arguments").GetProperty("text").GetProperty("value").GetString().ShouldEqual("example invalid");
    [Fact] void should_expose_the_argument_kind() => _diagnostic.GetProperty("arguments").GetProperty("text").GetProperty("kind").GetString().ShouldEqual("text");
    [Fact] void should_expose_the_introduced_error_subject() => _introduced.GetProperty("arguments").GetProperty("text").GetProperty("value").GetString().ShouldEqual("example invalid");
    [Fact] void should_serialize_introduced_arguments_like_the_diagnostics_view() => _introduced.GetProperty("arguments").GetRawText().ShouldEqual(_diagnostic.GetProperty("arguments").GetRawText());
}
