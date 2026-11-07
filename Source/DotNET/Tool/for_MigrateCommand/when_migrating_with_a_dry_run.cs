// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

public class when_migrating_with_a_dry_run : given.a_screenplay_5_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["migrate", Root, "--from", "5", "--dry-run"], Output, Error);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_write_nothing() => File.ReadAllText(Document).ShouldEqual(Screenplay5);
    [Fact] void should_name_the_plan() => Output.ToString().ShouldContain("Migrating 1 file from Screenplay 5 to 6 (1 step: 5→6 slots-and-outlets)");
    [Fact] void should_print_each_change_with_its_line() => Output.ToString().ShouldContain("4:3 navigation contributes Navigation → navigation + outlet Navigation of navigation in navigation");
    [Fact] void should_say_nothing_was_written() => Output.ToString().ShouldContain("Run again without --dry-run to write.");
}
