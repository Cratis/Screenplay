// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

/// <summary>
/// The two spellings of a clear share a block here on purpose - each has to come back out the way it went in,
/// so printing a document never rewrites the author's choice into the other one.
/// </summary>
public class when_printing_a_projection_that_clears_properties : given.a_printer
{
    const string Source =
        """
        projection Notes => NoteReadModel
          from NoteCleared
            clear note
            clear owner.note
            summary = null
        """;

    string _printed;
    CompilationResult<ProjectionSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _printed = _printer.Print(_compiler.CompileProjection(Source).Value!);
        _reparsed = _compiler.CompileProjection(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_print_the_clear_back_as_a_clear() => _printed.ShouldContain("clear note");
    [Fact] void should_print_the_dotted_path_back_as_a_clear() => _printed.ShouldContain("clear owner.note");
    [Fact] void should_not_print_a_clear_as_an_assignment_of_null() => _printed.ShouldNotContain("note = null");
    [Fact] void should_print_the_assignment_of_null_back_as_an_assignment() => _printed.ShouldContain("summary = null");
    [Fact] void should_reparse_the_clears_as_clear_mappings() => Mappings.OfType<ClearMappingSyntax>().Select(_ => _.Property).ShouldContainOnly("note", "owner.note");
    [Fact] void should_reparse_the_assignment_as_an_assignment() => Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("summary");

    IEnumerable<MappingSyntax> Mappings => _reparsed.Value!.Blocks.OfType<FromSyntax>().Single().Mappings;
}
