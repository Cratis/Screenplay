// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_structured_values;

public class and_a_string_needs_json_escaping : given.a_printer
{
    const string Source =
        """
        specification Placing
          when Place
            line = {"sku":"A\"1"}
        """;

    string _printed = string.Empty;
    CompilationResult<SpecificationSyntax> _reparsed;

    void Because()
    {
        _printed = _printer.Print(_compiler.CompileSpecification(Source).Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_preserve_canonical_json() => _printed.TrimEnd().ShouldEqual(Source);
    [Fact] void should_reparse_the_escaped_string() => _reparsed.Success.ShouldBeTrue();
}
