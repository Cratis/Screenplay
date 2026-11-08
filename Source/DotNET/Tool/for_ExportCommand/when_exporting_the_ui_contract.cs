// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Ui;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_the_ui_contract : Tool.given.a_renderable_model
{
    const string WithScreen = Source + """

                  screen Projects
                    data Projects[] via query GetProjects
        """;

    int _exitCode;
    UiContract _contract;

    void Establish() => Write(Root, WithScreen);

    void Because()
    {
        _exitCode = ToolCommands.Run(["export", Root, "--format", "ui"], Output, Error);
        _contract = UiContractJson.Read(Encoding.UTF8.GetBytes(Output.ToString()));
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_describe_the_screen() => _contract.Screens.Single().Name.ShouldEqual("Projects");
    [Fact] void should_route_its_data() => _contract.Screens.Single().Data.Single().Route.ShouldEqual("/api/projects/registration/list/get-projects");
    [Fact] void should_report_nothing() => Error.ToString().ShouldEqual(string.Empty);
}
