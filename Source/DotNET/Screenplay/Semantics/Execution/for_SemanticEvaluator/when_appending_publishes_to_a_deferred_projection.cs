// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_appending_publishes_to_a_deferred_projection : given.a_scoped_publication_plan
{
    void Establish() => Prepare(false);
    void Because() => _result = SemanticEvaluator.Append(_plan, SemanticWorld.Empty, _fact, [], null);

    [Fact] void should_refuse_projection_without_querying() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_identify_the_deferred_consumer() => ((SemanticUnsupported)_result).Details.ShouldContain("DeferredSummary");
    [Fact] void should_return_the_input_world() => _result.World.ShouldBeSame(SemanticWorld.Empty);
    [Fact] void should_not_commit_original_or_published_facts() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_not_commit_read_models() => _result.World.ReadModels.ShouldBeEmpty();
}
