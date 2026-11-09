// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model;

public class and_its_screens_cannot_be_admitted : an_export
{
    const string WithScreen = Source + """

              screen RegisterProject
                title "Register a project"
                action RegisterProject
                  navigate to Nowhere
        """;

    string _refusal;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), WithScreen);
        Initialize();
    }

    void Because()
    {
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString();
        _refusal = Refusal(new { expectedRevision = revision, view = "executable-model", screens = true });
    }

    [Fact] void should_refuse_with_the_reason() => _refusal.ShouldContain("ScreensRefused: Screen 'RegisterProject'");
}
