// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_reporting_mixed_top_level_dispositions : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        domain Sales.Invoicing
        layout ApplicationShell
          content
        persona Accountant
          policy Missing
        module Invoicing
          feature Invoices
        """);

    [Fact] void should_refuse_to_publish_a_model() => _result.Value.ShouldBeNull();
    [Fact] void should_report_authoring_metadata_before_deferred_metadata() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(
        DiagnosticCodes.ReportOnlySemanticSyntax,
        DiagnosticCodes.UnknownPolicy,
        DiagnosticCodes.ReportOnlySemanticSyntax,
        DiagnosticCodes.DeferredSemanticSyntax);
    [Fact] void should_attribute_findings_in_reporting_order_not_source_order() => _result.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual(1, 4, 4, 2);
}
