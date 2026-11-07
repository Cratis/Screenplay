// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

public class when_from_is_missing : given.a_screenplay_5_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["migrate", Root], Output, Error);

    [Fact] void should_fail() => _exitCode.ShouldEqual(1);
    [Fact] void should_say_why() => Error.ToString().ShouldContain("an old file does not say its version");
    [Fact] void should_write_nothing() => File.ReadAllText(Document).ShouldEqual(Screenplay5);
}
