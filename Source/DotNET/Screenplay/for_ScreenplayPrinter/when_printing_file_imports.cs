// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_file_imports : given.a_printer
{
    const string Source =
        """
        import Customers.CustomerRegistered
        import "**/*.play"

        module Ordering
          description "Orders"
          import "Orders/*.play"

          feature Orders
            import "Slices/**/*.play"

        """;

    string _printed;

    void Because() => _printed = _printer.Print(_compiler.Parse(Source).Value!);

    [Fact] void should_print_the_document_as_written() => _printed.ShouldEqual(Source);
}
