// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_registering_performed_queries_with_mixed_admission : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Billing
          feature Invoices
            slice StateView InvoiceFigures
              readmodel InvoiceFigures
                invoiceId String
                name String
              query CurrentFigures => InvoiceFigures[]
                performer
                  file Queries/CurrentFigures.cs
              query NamedFigures => InvoiceFigures[]
                by name String from $context.identity
                performer
                  file Queries/NamedFigures.cs
        """);

    [Fact] void should_refuse_to_publish_a_model() => _result.Value.ShouldBeNull();
    [Fact] void should_report_the_admitted_query_before_refusing_the_next() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        DiagnosticCodes.ReportOnlySemanticSyntax,
        DiagnosticCodes.ContextSourcedPerformedQueryArgument);
    [Fact] void should_keep_only_the_admitted_query_requirement() => _result.ImplementationRequirements.Single().Role.ShouldEqual(SemanticImplementationRole.QueryPerformer);
}
