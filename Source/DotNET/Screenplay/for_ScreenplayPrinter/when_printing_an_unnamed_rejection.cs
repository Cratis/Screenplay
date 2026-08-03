// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_an_unnamed_rejection : given.a_printer
{
    const string Source =
        """
        specification WhenExchangingAndMagicLinkIsNotActive
          when ExchangeToken
          then error
        """;

    CompilationResult<SpecificationSyntax> _original;
    string _printed;
    CompilationResult<SpecificationSyntax> _reparsed;

    void Because()
    {
        _original = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_compile_without_diagnostics() => _original.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_round_trip_unchanged() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);
    [Fact] void should_print_the_bare_form() => _printed.ShouldContain("then error\n");
    [Fact] void should_not_print_empty_quotes() => _printed.ShouldNotContain("then error \"\"");
    [Fact] void should_preserve_the_unnamed_reason() => _reparsed.Value!.ThenErrors.Single().Name.ShouldBeNull();
}
