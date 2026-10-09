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
    [Fact] void should_report_filter_scope_and_performer_as_unsupported() => _result.Diagnostics.ShouldContain(_ => _.Message.Contains("uses filtering, scope, or implementation behavior", StringComparison.Ordinal));
    [Fact] void should_report_context_sourced_by_argument_as_unsupported() => _result.Diagnostics.ShouldContain(_ => _.Message.Contains("must use caller-supplied 'by' arguments", StringComparison.Ordinal));
    [Fact] void should_keep_the_same_diagnostic_code() => _result.Diagnostics.Select(_ => _.Code).Distinct().ShouldEqual([DiagnosticCodes.UnsupportedSemanticSyntax]);
}
