// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_EnvironmentCommand;

public class when_the_model_has_errors : for_ModelCheck.given.a_model
{
    [Fact]
    void should_list_nothing_and_exit_with_the_defect_code()
    {
        ToolCommands.Run(["environment", Root], Output, Error).ShouldEqual(1);
        Output.ToString().ShouldBeEmpty();
        Error.ToString().ShouldContain("error-free");
    }
}
