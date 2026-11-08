// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_LineageCommand;

public class when_a_trace_has_breaks : given.the_library
{
    void Establish() => File.AppendAllText(Path.Combine(Root, "application.play"), "\nmodule Extra\n  feature Notes\n    slice StateView Shelf\n      readmodel ShelfEntry\n        label String\n");

    void Because() => ExitCode = ToolCommands.Run(["lineage", "Extra.Notes.Shelf.ShelfEntry.label", Root], Output, Error);

    [Fact] void should_exit_with_one() => ExitCode.ShouldEqual(1);
    [Fact] void should_name_the_missing_clause() => Output.ToString().ShouldContain("<- projection-mapping");
    [Fact] void should_summarize_the_break() => Output.ToString().ShouldContain("1 break(s)");
}
