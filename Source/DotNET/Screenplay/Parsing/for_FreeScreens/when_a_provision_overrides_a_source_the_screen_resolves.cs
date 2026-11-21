// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_FreeScreens;

public class when_a_provision_overrides_a_source_the_screen_resolves : given.a_free_screens_model
{
    void Establish() => Compile("screen Sourced\n  data InvoiceRow[] via query ListInvoices\n  table InvoiceRow\n    column amount\n\n" + Slice("uses screen Sourced\n  data InvoiceRow via query ListInvoices"));

    [Fact] void should_refuse_the_override() => With(DiagnosticCodes.ProvisionOverridesScreenSource).Count().ShouldEqual(1);
}
