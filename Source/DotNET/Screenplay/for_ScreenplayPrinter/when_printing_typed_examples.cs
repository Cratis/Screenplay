// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_typed_examples : given.a_printer
{
    const string Source =
        """
        example AcmeInvoice : M.F.InvoiceRegistered
          description "A known invoice"
          for "invoice-1"
          lines = [{"quantity":2}]
          total = 1000
          generated receipt = "11111111-1111-1111-1111-111111111111"
        specification Registering
          given M.F.AcmeInvoice total = 2000
            lines = []
          given readmodel M.F.Balance total = 2000
          when M.F.AcmeInput lines = [{"quantity":3}]
            total = 3000
          then M.F.AcmeInvoice total = 3000
          then readmodel M.F.Balance exactly total = 3000
        """;

    CompilationResult<SpecificationSyntax> _original;
    CompilationResult<SpecificationSyntax> _reparsed;
    SpecificationSyntax _fromJson;
    string _printed;

    void Because()
    {
        _original = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(_original.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
        _fromJson = (SpecificationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(_original.Value!));
    }

    [Fact] void should_parse_without_diagnostics() => _original.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_authored_syntax() => SyntaxJson.StructurallyEqual(_original.Value!, _reparsed.Value!).ShouldBeTrue();
    [Fact] void should_round_trip_through_syntax_json() => SyntaxJson.StructurallyEqual(_original.Value!, _fromJson).ShouldBeTrue();
    [Fact] void should_keep_the_inline_spelling() => _printed.ShouldContain("given M.F.AcmeInvoice total = 2000");
    [Fact] void should_keep_exact_read_model_matching() => _printed.ShouldContain("then readmodel M.F.Balance exactly total = 3000");
    [Fact] void should_print_identically_on_a_second_pass() => _printer.Print(_reparsed.Value!).ShouldEqual(_printed);

    [Fact]
    void should_preserve_examples_in_every_application_scope()
    {
        var original = _compiler.Parse("example Root : E\n  p = 1\nmodule M\n  example Module : E\n    p = 2\n  feature F\n    example Feature : E\n      p = 3\n    slice StateChange S\n      example Slice : E\n        p = 4");
        var fromJson = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(original.Value!));
        var reparsed = _compiler.Parse(_printer.Print(fromJson));
        reparsed.Diagnostics.ShouldBeEmpty();
        SyntaxJson.StructurallyEqual(original.Value!, reparsed.Value!).ShouldBeTrue();
    }

    [Fact]
    void should_preserve_an_inline_appended_event()
    {
        var original = _compiler.CompileSpecification("specification S\n  when append M.F.AcmeInvoice total = 1000\n  then readmodel M.F.Balance total = 1000");
        var reparsed = _compiler.CompileSpecification(_printer.Print(original.Value!));
        SyntaxJson.StructurallyEqual(original.Value!, reparsed.Value!).ShouldBeTrue();
    }
}
