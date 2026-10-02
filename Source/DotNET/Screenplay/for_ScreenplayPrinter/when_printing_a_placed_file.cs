// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_placed_file : given.a_printer
{
    const string Source =
        """
        description "Placing orders"
        import "Placing/*.play"

        slice StateChange PlaceOrder

          command PlaceOrder
            orderId Uuid identifier

        """;

    string _printed;

    void Because() => _printed = _printer.Print(_compiler.Parse(Source, "Orders.play", new PlayPlacement(["Ordering", "Orders"])).Value!);

    [Fact] void should_print_the_file_as_written() => _printed.ShouldEqual(Source);
}
