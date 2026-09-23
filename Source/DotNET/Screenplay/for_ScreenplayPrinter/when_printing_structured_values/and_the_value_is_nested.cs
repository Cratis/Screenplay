// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_structured_values;

public class and_the_value_is_nested : given.a_printer
{
    const string Source =
        """
        specification Placing
          when Place
            lines = [{"sku":"A-1","quantity":2,"tags":[]}]
        """;

    CompilationResult<SpecificationSyntax> _result;
    string _printed = string.Empty;
    CompilationResult<SpecificationSyntax> _reparsed;

    void Because()
    {
        _result = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(_result.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_parse_original() => _result.Success.ShouldBeTrue();
    [Fact] void should_print_inline_json() => _printed.Contains("""[{"sku":"A-1","quantity":2,"tags":[]}]""", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_reparse_without_error() => _reparsed.Success.ShouldBeTrue();
    [Fact] void should_be_stable() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);
}
