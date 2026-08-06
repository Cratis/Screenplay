// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_slice_with_several_projections : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceList
              projection InvoiceList => InvoiceListReadModel
                from InvoiceRegistered
                  status = "draft"

              projection InvoicePolicy => InvoicePolicyReadModel
                from InvoiceCancelled
                  cancelled = true
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_first_projection() => _roundtrip.Printed.ShouldContain("projection InvoiceList => InvoiceListReadModel");
    [Fact] void should_print_the_second_projection() => _roundtrip.Printed.ShouldContain("projection InvoicePolicy => InvoicePolicyReadModel");
    [Fact] void should_preserve_both_projections() => Slice.Projections.Select(_ => _.Name).ShouldContainOnly("InvoiceList", "InvoicePolicy");
    [Fact] void should_preserve_their_order() => Slice.Projections.First().Name.ShouldEqual("InvoiceList");

    SliceSyntax Slice => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single();
}
