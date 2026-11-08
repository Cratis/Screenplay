// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_a_model_with_errors : Tool.given.a_renderable_model
{
    int _exitCode;

    void Establish() => Write(Root, Source.Replace("name ProjectName", "name MissingType", StringComparison.Ordinal));

    void Because() => _exitCode = ToolCommands.Run(["export", Root], Output, Error);

    [Fact] void should_report_defects() => _exitCode.ShouldEqual(1);
    [Fact] void should_say_there_is_no_executable_model() => Error.ToString().ShouldContain("has no executable model");
    [Fact] void should_write_no_model() => Output.ToString().ShouldBeEmpty();
}
