// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_migrating_legacy_inline_fences : given.a_printer
{
    const string Source =
        """
        module Billing
          description
            ```
            Billing operations.
            Invoices and payments.
            ```
          feature Invoices
            slice StateChange Register
              command Register
                validate csharp
                  ```
                  yield return "invalid";
                  ```
                handler
                  csharp
                    ```
                    return [];
                    ```
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_accept_the_old_forms() => _roundtrip.Original!.Success.ShouldBeTrue();
    [Fact] void should_warn_for_each_old_form() => _roundtrip.Original!.Diagnostics.Select(_ => _.Code).Count(_ => _ == DiagnosticCodes.LegacyInlineCodeFence).ShouldEqual(3);
    [Fact] void should_emit_tagged_description() => _roundtrip.Printed.ShouldContain("```text");
    [Fact] void should_emit_tagged_validation() => _roundtrip.Printed.ShouldContain("validate\n          ```csharp");
    [Fact] void should_not_emit_the_old_validation() => _roundtrip.Printed.ShouldNotContain("validate csharp");
    [Fact] void should_emit_tagged_handler() => _roundtrip.Printed.ShouldContain("handler\n          ```csharp");
    [Fact] void should_reparse_without_warnings() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_stably() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
