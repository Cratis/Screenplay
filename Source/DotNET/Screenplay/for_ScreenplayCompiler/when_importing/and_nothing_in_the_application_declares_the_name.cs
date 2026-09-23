// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_importing;

public class and_nothing_in_the_application_declares_the_name : given.a_compiler
{
    const string Source =
        """
        import Billing.InvoiceView
        concept OrderId : Uuid
        module Shop
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId identifier
                reads InvoiceView by orderId
                produces OrderPlaced
                  for orderId
              event OrderPlaced
              specification PlacingAnOrder
                given readmodel InvoiceView
                  status = "anything"
                when PlaceOrder
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then OrderPlaced
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_leave_the_imported_shape_undecided_and_report_nothing() => _result.Diagnostics.ShouldBeEmpty();
}
