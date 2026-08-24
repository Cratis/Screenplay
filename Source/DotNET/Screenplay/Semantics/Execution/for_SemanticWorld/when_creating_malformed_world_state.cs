// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticWorld;

public class when_creating_malformed_world_state : Specification
{
    Exception _defaultEvent;
    Exception _defaultReadModel;

    void Because()
    {
        _defaultEvent = Catch.Exception(() => SemanticWorld.Create([new(default, SemanticValue.Null, [])], []));
        _defaultReadModel = Catch.Exception(() => SemanticWorld.Create([], [new(default, SemanticValue.Null, [])]));
    }

    [Fact] void should_reject_a_default_event_contract() => _defaultEvent.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_default_read_model() => _defaultReadModel.ShouldBeOfExactType<InvalidSemanticContract>();
}
