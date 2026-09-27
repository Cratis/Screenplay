// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_absent_read_models_with_comments : given.a_printer
{
    const string Source =
        """
        specification RemovingAnInvoice
          // This invoice was removed, not set to null.
          then no readmodel InvoiceView for "first"
          then InvoiceRemoved
            invoiceId = "first"
          // The other instance is still present.
          then readmodel InvoiceView
            invoiceId = "second"
        """;

    CompilationResult<SpecificationSyntax> _reparsed;
    string _printed;

    void Because()
    {
        var parsed = _compiler.CompileSpecification(Source);
        _printed = _printer.Print(parsed.Value!);
        _reparsed = _compiler.CompileSpecification(_printed);
    }

    [Fact] void should_keep_the_absence_and_present_assertions_in_order() => _printed.IndexOf("then no readmodel", StringComparison.Ordinal).ShouldBeLessThan(_printed.IndexOf("then readmodel", StringComparison.Ordinal));
    [Fact] void should_preserve_order_across_different_then_kinds() => _printed.IndexOf("then no readmodel", StringComparison.Ordinal).ShouldBeLessThan(_printed.IndexOf("then InvoiceRemoved", StringComparison.Ordinal));
    [Fact] void should_preserve_the_comments() => _printed.ShouldContain("// This invoice was removed, not set to null.");
    [Fact] void should_preserve_the_other_comment() => _printed.ShouldContain("// The other instance is still present.");
    [Fact] void should_round_trip_the_key() => _reparsed.Value!.ThenAbsentReadModels.Single().Name.ShouldEqual("InvoiceView");
    [Fact] void should_keep_the_present_assertion() => _reparsed.Value!.ThenReadModels!.Single().Name.ShouldEqual("InvoiceView");
}
