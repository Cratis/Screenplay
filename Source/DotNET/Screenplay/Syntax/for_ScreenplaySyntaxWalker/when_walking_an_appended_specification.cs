// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_an_appended_specification : Specification
{
    const string Source =
        """
        specification AppendingAnInvoice
          when append InvoiceRegistered
            for "9c858901-8a57-4791-81fe-4c455b099bc9"
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then readmodel InvoiceSummary exactly
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
        """;

    given.a_counting_walker _walker;
    SpecificationSyntax _specification;

    void Establish()
    {
        _walker = new();
        _specification = new ScreenplayCompiler().CompileSpecification(Source).Value!;
    }

    void Because() => _walker.VisitSpecification(_specification);

    [Fact] void should_reach_every_node() => _walker.Nodes.Count.ShouldEqual(given.SyntaxNodes.Under(_specification).Count);
    [Fact] void should_reach_the_appended_event() => _walker.Nodes.OfType<SpecificationEventSyntax>().Single().ShouldEqual(_specification.WhenAppended);
}
