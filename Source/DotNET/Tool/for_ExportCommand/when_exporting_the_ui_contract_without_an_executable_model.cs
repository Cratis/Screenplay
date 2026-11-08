// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Ui;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_the_ui_contract_without_an_executable_model : Tool.given.a_renderable_model
{
    const string Broken = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateView List
              readmodel Projects
                projectId ProjectId
              query GetProjects => Projects[]
              projection Broken => Missing
                from Nothing
                  projectId = $eventSourceId
              screen Projects
                data Projects[] via query GetProjects
        """;

    int _exitCode;
    UiContract _contract;

    void Establish() => Write(Root, Broken);

    void Because()
    {
        _exitCode = ToolCommands.Run(["export", Root, "--format", "ui"], Output, Error);
        _contract = UiContractJson.Read(Encoding.UTF8.GetBytes(Output.ToString()));
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_still_describe_the_screen() => _contract.Screens.Single().Data.Single().Query.ShouldEqual("GetProjects");
    [Fact] void should_say_identities_and_routes_are_missing() => Error.ToString().ShouldContain("carries no identities or routes");
}
