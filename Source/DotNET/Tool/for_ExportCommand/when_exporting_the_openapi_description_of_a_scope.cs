// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_the_openapi_description_of_a_scope : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--format", "openapi", "--scope", "Projects.Registration.List"], Output, Error);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_describe_the_query_in_scope() => Output.ToString().ShouldContain("\"/api/projects/registration/list/get-projects\"");
    [Fact] void should_leave_out_the_command_outside_the_scope() => Output.ToString().ShouldNotContain("register-project");
}
