// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_sample_data_with_nested_rows : given.a_printer
{
    const string Source = """
        type Line
          sku String
          children Line[]

        module Shop
          feature Baskets
            slice StateView BasketList
              readmodel Basket
                lines Line[]

        sample data Demo
          shape Basket
            row
              // The authored collection
              lines
                row
                  sku = "BOOK"
                  children
                    row
                      // A nested child
                      sku = "PEN"
                row
                  sku = "BAG"
            row
              // Explicitly empty
              lines
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_nested_rows() => _roundtrip.Printed.ShouldContain("lines\n        row\n          sku = \"BOOK\"\n          children\n            row\n              // A nested child\n              sku = \"PEN\"");
    [Fact] void should_preserve_the_collection_comment() => _roundtrip.Printed.ShouldContain("// The authored collection\n      lines");
    [Fact] void should_preserve_the_empty_collection() => _roundtrip.Reparsed.Value!.SampleDataPacks.Single().Sets.Single().Rows.Last().Fields.Single().Rows.ShouldBeEmpty();
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
