// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_RenameCommand;

public class when_renaming_an_unknown_address : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["rename", "Projects.Registration.Missing", "Other", Root], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_name_the_address() => Error.ToString().ShouldContain("'Projects.Registration.Missing' does not name a declaration.");
    [Fact] void should_write_no_identity_file() => Directory.Exists(Path.Combine(Root, ".screenplay")).ShouldBeFalse();
}
