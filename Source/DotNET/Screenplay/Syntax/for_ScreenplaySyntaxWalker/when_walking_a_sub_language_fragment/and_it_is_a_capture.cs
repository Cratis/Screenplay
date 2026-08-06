// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_sub_language_fragment;

public class and_it_is_a_capture : Specification
{
    const string Source =
        """
        # A capture using the full feature set of the sub-language
        capture LegacyInvoiceCapture
          source webhook
            path /invoices
          key id
          map
            status = status translate
              "utkast" => draft
              "sendt"  => sent
            split fullName by " "
              firstName
              lastName
            summary = `${status} invoice`
          append InvoiceStatusChanged
            when status
              invoiceId = $.id
              status    = $.status
          children lineItems identified by lineNumber
            map
              productName = name
            append InvoiceLineItemAdded
              when added
                invoiceId  = $.id
                lineNumber = $.lineNumber
          nested billingContact
            map
              contactName = name
            append BillingContactUpdated
              when email
                invoiceId = $.id
                email     = $.email
        """;

    given.a_counting_walker _walker;
    Captures.CaptureSyntax _capture;

    void Establish()
    {
        _walker = new();
        _capture = new ScreenplayCompiler().CompileCapture(Source).Value!;
    }

    void Because() => _walker.VisitCapture(_capture);

    [Fact] void should_reach_every_node_the_fragment_holds() => _walker.Nodes.Count.ShouldEqual(given.SyntaxNodes.Under(_capture).Count);
    [Fact] void should_reach_every_append() => _walker.Nodes.OfType<Captures.CaptureAppendSyntax>().Count().ShouldEqual(3);
}
