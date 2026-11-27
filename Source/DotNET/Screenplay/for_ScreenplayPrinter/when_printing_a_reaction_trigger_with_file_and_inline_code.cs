// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_reaction_trigger_with_file_and_inline_code : given.a_printer
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice Automation Reconcile
              event InvoicePaid

              reaction Reconciler
                when InvoicePaid
                  ```csharp
                  return;
                  ```
                  file Reactions/Reconcile.cs
        """;

    const string PrintedTrigger =
        """
                when InvoicePaid
                  file Reactions/Reconcile.cs
                  ```csharp
                  return;
                  ```
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_print_the_file_before_the_code_under_the_same_trigger() => _roundtrip.Printed.ShouldContain("\n" + PrintedTrigger + "\n");
    [Fact] void should_preserve_the_trigger_body_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
