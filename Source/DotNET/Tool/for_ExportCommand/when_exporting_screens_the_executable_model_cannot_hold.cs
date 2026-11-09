// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_screens_the_executable_model_cannot_hold : Tool.given.a_renderable_model
{
    const string WithScreen = Source + """

                  screen Projects
                    title "Projects"
                    data Projects[] via query GetProjects
                    navigate to Nowhere
        """;

    int _exitCode;

    void Establish() => Write(Root, WithScreen);

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--screens"], Output, Error);

    [Fact] void should_fail() => _exitCode.ShouldEqual(1);
    [Fact] void should_name_the_refusal() => Error.ToString().ShouldContain("leads to 'Nowhere'");
    [Fact] void should_write_no_model() => Output.ToString().ShouldEqual(string.Empty);
}
