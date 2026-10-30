// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_it_is_observable_and_single : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String key
                total Int
              query LiveFigure => observable InvoiceFigures
                by invoiceId String
                performer
                  file Queries/LiveFigure.cs
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticImplementationRequirement _requirement;

    void Because()
    {
        _result = Bind(Source);
        _requirement = _result.ImplementationRequirements.Single(requirement => requirement.Role == SemanticImplementationRole.QueryPerformer);
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_be_live() => _requirement.QueryShape!.Delivery.ShouldEqual(SemanticQueryDelivery.Live);
    [Fact] void should_return_one() => _requirement.QueryShape!.Cardinality.ShouldEqual(SemanticQueryCardinality.One);
    [Fact] void should_keep_the_first_result_version() => _requirement.ResultVersion.ShouldEqual(1u);
}
