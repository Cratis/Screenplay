// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_event_tag_expectations : Specification
{
    CompilationResult<SpecificationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        var parsed = compiler.CompileSpecification("specification S\n  when C\n  then E\n    tag audit\n    tag \"billing team\"\n    tag audit\n  then E\n    no tags\n  then E");
        _printed = new ScreenplayPrinter().Print(parsed.Value!);
        _reparsed = compiler.CompileSpecification(_printed);
    }

    [Fact] void should_print_parseable_expectations() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_order_and_duplicates() => _reparsed.Value!.ThenEvents.First().Tags.Select(tag => ((LiteralExpressionSyntax)tag.Value).Value).ShouldEqual(["audit", "billing team", "audit"]);
    [Fact] void should_keep_an_explicit_empty_expectation() => _reparsed.Value!.ThenEvents.ElementAt(1).NoTags.ShouldNotBeNull();
    [Fact] void should_keep_absent_expectations_distinct() => _reparsed.Value!.ThenEvents.Last().NoTags.ShouldBeNull();
    [Fact] void should_print_stably() => new ScreenplayPrinter().Print(_reparsed.Value!).ShouldEqual(_printed);
}
