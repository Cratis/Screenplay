// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_the_cardinality_differs : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("query OneInvoice => InvoiceRow\nuses screen InvoiceList\n  data Invoice via query OneInvoice\n    number from invoiceNumber"));

    [Fact] void should_report_the_mismatch() => With(DiagnosticCodes.ScreenNeedCardinalityMismatch).Single().Message.ShouldContain("a list");
}
