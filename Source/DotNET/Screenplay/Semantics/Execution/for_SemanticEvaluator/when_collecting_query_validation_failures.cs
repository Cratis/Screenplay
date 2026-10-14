// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_collecting_query_validation_failures : Specification
{
    const string Source = """
        concept ItemId : String
          validate
            min 2 message "Invalid key"
        concept Status : String
          validate
            min 2 message "Invalid status"
        concept Category : String
          validate
            min 2 message "Invalid category"
        module Items
          feature Browsing
            slice StateView Browse
              readmodel Item
                itemId ItemId
                status Status
                category Category
              query Filtered => Item[]
                by itemId ItemId
                filter status Status optional
                filter category Category optional
        """;
    SemanticRejected _rejected;

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(query_filter_source.Compile(Source).Model).Plan!;
        var query = plan.Queries.Values.Single();
        var request = new SemanticQueryRequest(query.Id, SemanticValue.Text("a"))
        {
            Filters = [new(query.Filters[1].Id, SemanticValue.Text("c")), new(query.Filters[0].Id, SemanticValue.Text("s"))]
        };
        _rejected = (SemanticRejected)new SemanticEvaluator().Execute(plan, SemanticWorld.Empty,
            new SemanticExecutionRequest(default, [], [request], ImmutableDictionary<SemanticId, SemanticValue>.Empty) { IsReadOnly = true });
    }

    [Fact] void should_validate_the_key_then_filters_in_declaration_order() => _rejected.ValidationFailures.Select(failure => failure.Message).ShouldEqual(new[] { "Invalid key", "Invalid status", "Invalid category" });
    [Fact] void should_report_the_key_failure_first() => _rejected.Details.ShouldEqual("Invalid key");
    [Fact] void should_retain_the_original_world() => _rejected.World.ShouldBeSame(SemanticWorld.Empty);
}
