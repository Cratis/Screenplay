// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_skipping_bodies_in_exact_mode
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("then no result")]
    [InlineData("then events in any order")]
    [InlineData("given clock \"2026-10-05T08:00:00Z\"")]
    [InlineData("when clock \"2026-10-05T08:00:00Z\"")]
    void should_reject_a_nested_numeric_directive(string header)
    {
        var result = _compiler.Parse("numbers exact\n" + Wrap($"{header}\n    numbers exact"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(d => d.Code == DiagnosticCodes.InvalidNumericDirective).ShouldBeTrue();
    }

    [Theory]
    [InlineData("then no result")]
    [InlineData("then events in any order")]
    [InlineData("given clock \"2026-10-05T08:00:00Z\"")]
    [InlineData("when clock \"2026-10-05T08:00:00Z\"")]
    void should_keep_skipping_a_nested_numeric_directive_silently_for_unmarked_source(string header)
    {
        var result = _compiler.Parse(Wrap($"{header}\n    numbers exact"));
        result.Diagnostics.Any(d => d.Code == DiagnosticCodes.InvalidNumericDirective).ShouldBeFalse();
    }

    [Fact]
    void should_not_read_a_fenced_line_or_a_property_named_numbers_as_a_directive()
    {
        var result = _compiler.Parse("numbers exact\n" + "module M\n  feature F\n    slice StateChange S\n      specification Sp\n        then no result\n          ```text\nnumbers exact\n          ```\n          numbers = 1\n");
        result.Diagnostics.Any(d => d.Code == DiagnosticCodes.InvalidNumericDirective).ShouldBeFalse();
    }

    static string Wrap(string body) => "module M\n  feature F\n    slice StateChange S\n      specification Sp\n        " + body.Replace("\n", "\n        ") + "\n";
}
