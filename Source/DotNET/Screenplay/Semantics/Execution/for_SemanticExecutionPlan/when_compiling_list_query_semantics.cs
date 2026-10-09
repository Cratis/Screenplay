// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticExecutionPlan;

public class when_compiling_list_query_semantics : a_semantic_binder
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
              query AllWorkItems => observable WorkItemSummary[]
              query WorkItemsForStatus => observable WorkItemSummary[]
                by title String
        """;

    SemanticExecutionPlanCompilation _result;

    void Because()
    {
        var result = Bind(Source);
        result.Success.ShouldBeTrue();
        _result = SemanticExecutionPlan.Compile(result.Value!.Model);
    }

    [Fact] void should_compile_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_index_the_unkeyed_list_query() => _result.Plan!.Queries.Values.Single(_ => _.Name == "AllWorkItems").Argument.ShouldBeNull();
    [Fact] void should_index_the_keyed_list_query() => _result.Plan!.Queries.Values.Single(_ => _.Name == "WorkItemsForStatus").Argument!.Name.ShouldEqual("title");
}
