// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_standalone_projection_hash_comments : given.a_printer
{
    const string Source =
        """
        # @public projection
        projection OrderList
          # event annotation
          from OrderPlaced # @owner sales
            status = status
        """;

    CompilationResult<ProjectionSyntax> _reparsed;
    string _printed;

    void Because()
    {
        var projection = _compiler.CompileProjection(Source).Value!;
        _printed = _printer.Print(projection);
        _reparsed = _compiler.CompileProjection(_printed);
    }

    [Fact] void should_reparse() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_keep_the_hash_comments() => _printed.ShouldContain("# @owner sales");
    [Fact] void should_keep_the_leading_comment() => _printed.ShouldContain("# @public projection");
    [Fact] void should_be_stable() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);
}
