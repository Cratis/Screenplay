// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_an_observable_query : given.a_printer
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateView InvoiceList
              query LiveInvoices => observable InvoiceListReadModel[]
                description "Every invoice, kept current while the board is open"

              query ListInvoices => InvoiceListReadModel[]
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_observable_marker() => _roundtrip.Printed.ShouldContain("query LiveInvoices => observable InvoiceListReadModel[]");
    [Fact] void should_not_print_a_marker_on_a_one_shot_query() => _roundtrip.Printed.ShouldContain("query ListInvoices => InvoiceListReadModel[]");
    [Fact] void should_preserve_the_marker() => Query("LiveInvoices").IsObservable.ShouldBeTrue();
    [Fact] void should_preserve_the_absence_of_the_marker() => Query("ListInvoices").IsObservable.ShouldBeFalse();
    [Fact] void should_preserve_the_return_type() => Query("LiveInvoices").ReturnType.Name.ShouldEqual("InvoiceListReadModel");

    QuerySyntax Query(string name) =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single(_ => _.Name == name);
}
