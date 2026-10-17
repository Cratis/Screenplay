// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_the_command_handler_refusal : given.a_connection
{
    const string Model = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                handler
                  file Register.cs
        """;

    JsonElement _diagnostic;
    JsonElement _diagnostics;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
    }

    void Because()
    {
        var revision = Call("open-workspace").GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString();
        _diagnostics = Call("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items");
        _diagnostic = _diagnostics.EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0800");
    }

    [Fact] void should_not_report_the_handler_as_awaiting_an_esm_version() => _diagnostics.EnumerateArray().Any(item => item.GetProperty("code").GetString() == "PLAY0268").ShouldBeFalse();
    [Fact] void should_expose_the_subject_argument_kind() => _diagnostic.GetProperty("arguments").GetProperty("subject").GetProperty("kind").GetString().ShouldEqual("declaration");
    [Fact] void should_identify_the_command_declaration() => _diagnostic.GetProperty("arguments").GetProperty("subject").GetProperty("value").GetProperty("kind").GetString().ShouldEqual("command");
    [Fact] void should_expose_the_command_path() => _diagnostic.GetProperty("arguments").GetProperty("subject").GetProperty("value").GetProperty("path").EnumerateArray().Select(part => part.GetString()).ShouldEqual(["Projects", "Registration", "Register", "Register"]);
}
