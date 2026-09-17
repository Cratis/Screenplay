// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_projection_with_variants : given.a_printer
{
    const string Source =
        """
        projection WorkItem
          from TitleChanged
            title = title
          variant BacklogItem
            enters on IssueCreated
          variant PullRequestItem
            enters on PullRequestCreated key issueId
            from BuildCompleted
              buildStatus = status
        """;

    CompilationResult<ProjectionSyntax> _original;
    string _printed;
    CompilationResult<ProjectionSyntax> _reparsed;
    string _printedAgain;

    void Because()
    {
        _original = _compiler.CompileProjection(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.CompileProjection(_printed);
        _printedAgain = _printer.Print(_reparsed.Value!);
    }

    [Fact] void should_reparse_successfully() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_preserve_the_block_count() => _reparsed.Value!.Blocks.Count().ShouldEqual(_original.Value!.Blocks.Count());
    [Fact] void should_preserve_every_variant_name() =>
        Variants(_reparsed).Select(_ => _.Name).ShouldContainOnly([.. Variants(_original).Select(_ => _.Name)]);

    [Fact]
    void should_preserve_the_keyed_entering_event()
    {
        var entersOn = Variants(_reparsed).Single(_ => _.Name == "PullRequestItem").EntersOn.Single();
        entersOn.Event.ShouldEqual("PullRequestCreated");
        entersOn.Key.ShouldNotBeNull();
    }

    static IEnumerable<ProjectionVariantSyntax> Variants(CompilationResult<ProjectionSyntax> result) =>
        result.Value!.Blocks.OfType<ProjectionVariantSyntax>();
}
