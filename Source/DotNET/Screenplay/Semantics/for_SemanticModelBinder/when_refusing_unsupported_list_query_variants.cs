// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_refusing_unsupported_list_query_variants : given.a_semantic_binder
{
    const string Source =
        """
        concept WorkItemId : Uuid
        module Workspaces
          feature Boards
            slice StateView WorkItemList
              readmodel WorkItemSummary
                workItemId WorkItemId
                title String
              query FilteredWorkItems => observable WorkItemSummary[]
                filter status String optional
              query ScopedWorkItems => WorkItemSummary[]
                scoped to identity
              query ContextWorkItem => WorkItemSummary?
                by workItemId WorkItemId from $context.identity.id
              query PerformedWorkItems => WorkItemSummary[]
                performer
                  file Queries/PerformedWorkItems.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_not_bind_successfully() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_unresolved_filter_property() => _result.Diagnostics.ShouldContain(_ => _.Message.Contains("Query 'FilteredWorkItems' filter 'status' has no same-named read-model property", StringComparison.Ordinal));
    [Fact] void should_report_scope_as_unsupported() => _result.Diagnostics.ShouldContain(_ => _.Message.Contains("Query 'ScopedWorkItems' uses 'scoped to'", StringComparison.Ordinal));
    [Fact] void should_not_refuse_the_performer_query() => _result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldNotContain(_ => _.Message.Contains("PerformedWorkItems", StringComparison.Ordinal));
    [Fact] void should_report_the_performer_query_as_an_opaque_read() => _result.Diagnostics.ShouldContain(_ => _.Severity == DiagnosticSeverity.Information && _.Message.Contains("Query 'PerformedWorkItems' is performed by code", StringComparison.Ordinal));
    [Fact] void should_report_context_sourced_by_argument_as_unsupported() => _result.Diagnostics.ShouldContain(_ => _.Message.Contains("must use caller-supplied 'by' arguments", StringComparison.Ordinal));
    [Fact] void should_keep_the_same_diagnostic_code() => _result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).Select(_ => _.Code).Distinct().ShouldEqual([DiagnosticCodes.UnsupportedSemanticSyntax]);
}
