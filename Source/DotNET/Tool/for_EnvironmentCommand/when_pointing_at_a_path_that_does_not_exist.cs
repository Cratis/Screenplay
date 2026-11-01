// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_EnvironmentCommand;

public class when_pointing_at_a_path_that_does_not_exist : given.a_model_with_providers
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["environment", Path.Combine(Root, "missing")], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
}
