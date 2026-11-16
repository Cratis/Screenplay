// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_field_the_screen_reads_is_missing : given.a_free_screens_model
{
    void Establish() => Compile(Shapes + "\n" + FreeScreen + "\n" + Slice("uses screen InvoiceList\n  data Invoice via query ListInvoices"));

    [Fact] void should_report_the_unmapped_field() => With(DiagnosticCodes.ProvisionFieldMissing).Single().Message.ShouldContain("'number'");
    [Fact] void should_not_report_the_field_that_matches() => With(DiagnosticCodes.ProvisionFieldMissing).Count().ShouldEqual(1);
}
