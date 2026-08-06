// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_sub_language_fragment;

public class and_it_is_a_specification : Specification
{
    const string Source =
        """
        specification RegisteringADraftInvoice
          given CustomerRegistered
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name       = "Acme Corp"
          when RegisterInvoice
            invoiceId  = "9c858901-8a57-4791-81fe-4c455b099bc9"
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then InvoiceRegistered
            invoiceId  = "9c858901-8a57-4791-81fe-4c455b099bc9"
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then error "An invoice must have at least one line"
        """;

    given.a_counting_walker _walker;
    Specifications.SpecificationSyntax _specification;

    void Establish()
    {
        _walker = new();
        _specification = new ScreenplayCompiler().CompileSpecification(Source).Value!;
    }

    void Because() => _walker.VisitSpecification(_specification);

    [Fact] void should_reach_every_node_the_fragment_holds() => _walker.Nodes.Count.ShouldEqual(given.SyntaxNodes.Under(_specification).Count);
    [Fact] void should_reach_the_expected_rejection() => _walker.Nodes.OfType<Specifications.SpecificationErrorSyntax>().Count().ShouldEqual(1);
}
