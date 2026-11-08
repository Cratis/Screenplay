// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_an_unknown_scope : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--scope", "Projects.Missing"], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_name_the_scope() => Error.ToString().ShouldContain("Scope 'Projects.Missing' does not name a feature or slice");
    [Fact] void should_write_no_model() => Output.ToString().ShouldBeEmpty();
}
