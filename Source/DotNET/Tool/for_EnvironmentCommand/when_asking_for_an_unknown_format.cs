// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_EnvironmentCommand;

public class when_asking_for_an_unknown_format : given.a_model_with_providers
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["environment", Root, "--format", "xml"], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_list_nothing() => Output.ToString().ShouldBeEmpty();
}
