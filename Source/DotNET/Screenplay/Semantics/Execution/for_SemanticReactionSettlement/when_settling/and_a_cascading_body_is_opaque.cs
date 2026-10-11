// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_cascading_body_is_opaque : given.a_settlement
{
    SemanticUnsupported _unsupported;

    void Establish() => Prepare(
        """
        reaction Continue
          when Started
            produces Continued
        reaction Finish
          when Continued
            ```csharp
              return [];
              ```
        """);

    void Because() => _unsupported = (SemanticUnsupported)_settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_discard_the_partial_cascade_world() => _unsupported.World.ShouldBeSame(_world);
    [Fact] void should_preserve_the_missing_capability() => _unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
    [Fact] void should_preserve_the_opaque_body_details() => _unsupported.Details.ShouldEqual("Reaction 'Finish' has an opaque body and requires a target provider to run it.");
    [Fact] void should_leave_the_input_world_unchanged() => _world.Facts.AsEnumerable().ShouldEqual([_input]);
}
