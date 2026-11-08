// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool.for_DiffCommand;

public class when_passing_one_version : given.two_model_versions
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["diff", Root], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_print_the_usage() => Error.ToString().ShouldContain(CliCommandCatalog.Diff.Usage);
}
