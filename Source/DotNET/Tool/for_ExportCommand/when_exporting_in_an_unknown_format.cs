// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_in_an_unknown_format : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--format", "yaml"], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_print_the_usage() => Error.ToString().ShouldContain(CliCommandCatalog.Export.Usage);
}
