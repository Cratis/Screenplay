// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_the_executable_model_with_screens : Tool.given.a_renderable_model
{
    const string WithScreen = Source + """

                  screen Projects
                    title "Projects"
                    data Projects[] via query GetProjects
        """;

    int _exitCode;
    ExecutableSemanticModel _model;

    void Establish() => Write(Root, WithScreen);

    void Because()
    {
        _exitCode = ToolCommands.Run(["export", Root, "--screens"], Output, Error);
        _model = SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(Output.ToString()));
    }

    SemanticScreen Screen => _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens).Single();

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_select_esm_v42() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V42);
    [Fact] void should_hold_the_screen() => Screen.Name.ShouldEqual("Projects");
    [Fact] void should_key_its_data_to_the_query() => _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Queries).Single(query => query.Name == "GetProjects").Id.ShouldEqual(Screen.Data.Single().Query);
    [Fact] void should_report_nothing() => Error.ToString().ShouldEqual(string.Empty);
}
