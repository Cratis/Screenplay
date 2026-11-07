// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

public class when_no_step_leaves_the_version : given.a_screenplay_5_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["migrate", Root, "--from", "4"], Output, Error);

    [Fact] void should_fail() => _exitCode.ShouldEqual(1);
    [Fact] void should_name_the_gap() => Error.ToString().ShouldContain("No migration step goes from Screenplay 4");
    [Fact] void should_say_to_migrate_from_5() => Error.ToString().ShouldContain("a model written for an earlier version migrates from 5");
    [Fact] void should_write_nothing() => File.ReadAllText(Document).ShouldEqual(Screenplay5);
}
