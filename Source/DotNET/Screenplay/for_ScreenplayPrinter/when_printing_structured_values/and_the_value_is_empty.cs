// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_structured_values;

public class and_the_value_is_empty : given.a_printer
{
    const string Source =
        """
        specification Placing
          when Place
            first = {}
            second = []
        """;

    string _printed = string.Empty;
    CompilationResult<SpecificationSyntax> _reparsed;

    void Because()
    {
        _printed = _printer.Print(_compiler.CompileSpecification(Source).Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_keep_the_empty_object() => _printed.Contains("first = {}", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_empty_list() => _printed.Contains("second = []", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_reparse() => _reparsed.Success.ShouldBeTrue();
}
