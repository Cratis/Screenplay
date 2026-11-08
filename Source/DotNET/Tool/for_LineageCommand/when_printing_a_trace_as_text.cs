// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_LineageCommand;

public class when_printing_a_trace_as_text : given.the_library
{
    string[] _lines = [];

    void Because()
    {
        ExitCode = ToolCommands.Run(["lineage", Title, Root], Output, Error);
        _lines = Output.ToString().ReplaceLineEndings("\n").Split('\n');
    }

    [Fact] void should_succeed() => ExitCode.ShouldEqual(0);
    [Fact] void should_name_the_subject_and_direction() => _lines[0].ShouldEqual($"Lineage of {Title} (Property), both");
    [Fact] void should_show_where_the_value_goes() => _lines.Any(line => line.StartsWith("  -> production-mapping Lending.Catalog.AddBook.AddBook 'title = title'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_show_where_the_value_comes_from() => _lines.Any(line => line.StartsWith("  <- screen-input Lending.AddBookInput 'field title'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_end_with_the_summary() => _lines.ShouldContain("9 hop(s), complete.");
}
