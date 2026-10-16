// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDecisionReadAdmission;

public class when_inspecting_malformed_producers : Specification
{
    [Fact]
    void should_reject_uninitialized_producer_collections_as_contract_errors()
    {
        var sliceAddress = SemanticAddress.ForSlice(ApplicationIdentity.Create("Projects"), "M", "F", "Malformed");
        var reactionId = SemanticId.Create(SemanticAddress.ForReaction(sliceAddress, "Malformed"));
        var automation = new SemanticSlice(SemanticId.Create(sliceAddress), "Malformed", SemanticSliceKind.Automation, [], [], [], [], [], [])
        {
            Reactions = [new(reactionId, "Malformed", default)]
        };
        var error = Catch.Exception(() => _ = SemanticDecisionReadAdmission.Producers([automation]).ToArray());
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("producer collections");
    }
}
