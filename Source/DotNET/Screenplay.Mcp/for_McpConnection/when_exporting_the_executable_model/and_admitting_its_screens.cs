// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_admitting_its_screens : an_export
{
    const string WithScreen = Source + """

              screen RegisterProject
                title "Register a project"
                action RegisterProject
        """;

    JsonElement _default;
    JsonElement _admitted;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), WithScreen);
        Initialize();
    }

    void Because()
    {
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString();
        _default = Content("read-workspace", new { expectedRevision = revision, view = "executable-model" });
        _admitted = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", screens = true });
    }

    [Fact] void should_keep_the_default_version() => _default.GetProperty("semanticVersion").GetString().ShouldNotEqual("42.0");
    [Fact] void should_select_esm_v42() => _admitted.GetProperty("semanticVersion").GetString().ShouldEqual("42.0");
    [Fact] void should_pin_the_admitted_revision() => _admitted.GetProperty("modelRevision").GetString().ShouldNotEqual(_default.GetProperty("modelRevision").GetString());
}
