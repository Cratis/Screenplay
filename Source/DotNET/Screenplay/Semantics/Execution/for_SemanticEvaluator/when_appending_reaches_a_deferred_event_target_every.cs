// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_appending_reaches_a_deferred_event_target_every : given.a_scoped_publication_plan
{
    void Establish() => Prepare(true);
    void Because() => _result = SemanticEvaluator.Append(_plan, SemanticWorld.Empty, _fact, [], null);

    [Fact] void should_refuse_projection() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_identify_the_deferred_publisher() => ((SemanticUnsupported)_result).Details.ShouldContain("Publisher");
    [Fact] void should_return_the_input_world() => _result.World.ShouldBeSame(SemanticWorld.Empty);
    [Fact] void should_not_commit_facts() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_not_commit_read_models() => _result.World.ReadModels.ShouldBeEmpty();
}
