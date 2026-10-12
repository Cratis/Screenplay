// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_editing_never_persisted_event_properties : given.a_property_generation_connection
{
    void Because() => Evolve(2, false);

    [Fact] void should_match_the_in_place_result_json_snapshot() => given.property_result_snapshots.Verify("in-place-events", Proposal.GetProperty("property"));
    [Fact] void should_disclose_equal_revision_pairs() => Proposal.GetProperty("property").GetProperty("generations").EnumerateArray().All(entry => entry.GetProperty("from").GetUInt32() == entry.GetProperty("to").GetUInt32()).ShouldBeTrue();
    [Fact] void should_keep_current_property_identities_after_apply() => Property(Reopened, "Audited", "label", 2).SemanticId.ShouldEqual(Property(Before, "Audited", "name", 2).SemanticId);
    [Fact] void should_not_modify_historical_identity() => Property(Reopened, "Audited", "name", 1).SemanticId.ShouldEqual(Property(Before, "Audited", "name", 1).SemanticId);
}
