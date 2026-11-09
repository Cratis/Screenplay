// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ExportCommand;

public class when_exporting_screens_in_another_format : Tool.given.a_renderable_model
{
    int _exitCode;

    void Establish() => Write(Root, Source);

    void Because() => _exitCode = ToolCommands.Run(["export", Root, "--format", "openapi", "--screens"], Output, Error);

    [Fact] void should_refuse_the_usage() => _exitCode.ShouldEqual(2);
    [Fact] void should_print_the_usage() => Error.ToString().ShouldContain("Usage:");
}
