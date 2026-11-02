// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_structured_values;

public class and_it_holds_every_shape : given.a_printer
{
    const string Source =
        """
        module M
          feature F
            slice StateView S
              screen S
                component Cratis.Blueprint.Default.list items
                  property columns
                    item
                      id = "received"
                      "display name" = "Received"
                      @item = 1
                    item none
                    item empty
                    item
                      item "nested"
                  property emptyState empty
                  property rowActions none
                  property filter from literal mode oneWay
                    status = "open"
        """;

    const string Printed =
        """
                component Cratis.Blueprint.Default.list items
                  property columns
                    item
                      id = "received"
                      "display name" = "Received"
                      @item = 1
                    item none
                    item empty
                    item
                      item "nested"
                  property emptyState empty
                  property rowActions none
                  property filter from literal mode oneWay
                    status = "open"
        """;

    CompilationResult<ApplicationSyntax> _result;
    string _printed = string.Empty;
    CompilationResult<ApplicationSyntax> _reparsed;

    void Because()
    {
        _result = _compiler.Parse(Source);
        _printed = _printer.Print(_result.Value!);
        _reparsed = _compiler.Parse(_printed);
    }

    [Fact] void should_parse_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_component_as_authored() => _printed.ShouldContain(Printed);
    [Fact] void should_print_no_json() => _printed.ShouldNotContain("= [");
    [Fact] void should_round_trip_the_tree() => SyntaxJson.StructurallyEqual(_result.Value!, _reparsed.Value!).ShouldBeTrue();
}
