// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_repeating_automap_directives_with_comments : given.a_printer
{
    const string Source = """
        projection Orders => OrderList
          automap // projection first
          no automap // projection second
          from OrderPlaced
          every
            automap // every first
            no automap // every second
          all
            automap // all first
            no automap // all second
          children items identified by id
            automap // children first
            no automap // children second
            from ItemAdded
          nested details
            automap // nested first
            no automap // nested second
            from DetailAdded
          join details on id
            with DetailsJoined
              automap // join first
              no automap // join second
        """;

    [Fact]
    void should_warn_for_each_repeated_automap_and_keep_each_comment_separate()
    {
        var parsed = _compiler.CompileProjection(Source);
        var printed = _printer.Print(parsed.Value!);
        var reparsed = _compiler.CompileProjection(printed);

        parsed.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedProjectionAutoMap).ShouldEqual(6);
        reparsed.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedProjectionAutoMap).ShouldEqual(6);
        foreach (var scope in new[] { "projection", "every", "all", "children", "nested", "join" })
        {
            printed.ShouldContain($"automap // {scope} first");
            printed.ShouldContain($"no automap // {scope} second");
            printed.ShouldNotContain($"no automap // {scope} first // {scope} second");
        }

        _printer.Print(reparsed.Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_not_reprint_previous_automap_settings_after_a_typed_mode_edit()
    {
        var parsed = _compiler.CompileProjection(Source);
        var edited = parsed.Value! with { AutoMap = AutoMapMode.Enabled };
        var printed = _printer.Print(edited);
        var reparsed = _compiler.CompileProjection(printed);

        printed.Split('\n').Count(line => line.StartsWith("  automap", StringComparison.Ordinal)).ShouldEqual(1);
        printed.ShouldNotContain("  no automap // projection second");
        printed.ShouldContain("automap // projection second\n  // projection first");
        reparsed.Value!.AutoMap.ShouldEqual(AutoMapMode.Enabled);
        _printer.Print(reparsed.Value).ShouldEqual(printed);
    }
}
