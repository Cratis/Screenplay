// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_SourceOptionsParser;

public class when_rejecting_nested_numeric_directives
{
    readonly ScreenplayCompiler _compiler = new();

    [Theory]
    [InlineData("theme T\n  numbers exact\n")]
    [InlineData("numbers exact\n")]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      numbers exact\n")]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      specification Sp\n        given caller\n          numbers exact\n")]
    [InlineData("reaction R\n  when E\n    produces X\n      numbers exact\n")]
    [InlineData("reaction R\n  when E\n    invokes C\n      numbers legacy\n")]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          numbers\n")]
    void should_reject_a_directive_below_the_top_level_of_an_exact_document(string tail)
    {
        var source = tail == "numbers exact\n" ? "numbers exact\nnumbers exact\n" : "numbers exact\n" + tail;
        var result = _compiler.Parse(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(d => d.Code is DiagnosticCodes.InvalidNumericDirective or DiagnosticCodes.DuplicateNumericDirective).ShouldBeTrue();
    }

    [Theory]
    [InlineData("theme T\n  numbers exact\n")]
    [InlineData("reaction R\n  when E\n    produces X\n      numbers exact\n")]
    void should_leave_a_nested_directive_alone_in_legacy_documents(string source) =>
        _compiler.Parse(source).Diagnostics.Any(d => d.Code == DiagnosticCodes.InvalidNumericDirective).ShouldBeFalse();

    [Fact]
    void should_not_read_fenced_text_or_property_forms_as_directives()
    {
        var result = _compiler.Parse("numbers exact\ntheme T\n  ```text\n  numbers exact\n  ```\n  numbers = 1\n  numbers: 2\n  numbers Int32\n");
        result.Diagnostics.Any(d => d.Code == DiagnosticCodes.InvalidNumericDirective).ShouldBeFalse();
    }
}
