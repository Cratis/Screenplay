// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRepairCapabilities.when_reading_repair_capabilities;

public class and_actions_carry_review_metadata : given.repair_capabilities
{
    [Fact] void should_keep_the_payload_copy_removal_out_of_fix_all() => Action("PLAY0469").GetProperty("canFixAll").GetBoolean().ShouldBeFalse();
    [Fact] void should_require_individual_review_of_the_payload_copy_removal() => Action("PLAY0469").GetProperty("individualReviewRequired").GetBoolean().ShouldBeTrue();
    [Fact] void should_disclose_the_event_contract_change() => Action("PLAY0469").GetProperty("changesEventContract").GetBoolean().ShouldBeTrue();
    [Fact] void should_disclose_the_retired_addresses() => Action("PLAY0469").GetProperty("retiresSemanticAddresses").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_pin_evidence_for_the_payload_copy_removal() => Action("PLAY0469").GetProperty("pinRepairEvidence").GetBoolean().ShouldBeFalse();
    [Fact] void should_disclose_the_routing_change() => Action("PLAY0478").GetProperty("changesRouting").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_pinned_evidence_for_the_routing_repairs() => new[] { "PLAY0166", "PLAY0478" }.All(code => Action(code).GetProperty("pinRepairEvidence").GetBoolean()).ShouldBeTrue();
    [Fact] void should_advertise_the_timeline_repair() => Action("PLAY0516").GetProperty("requiredFormatting").GetString().ShouldEqual("CanonicalizeTouchedDocuments");
    [Fact] void should_advertise_the_redundant_route_removal_without_a_routing_change() => Action("PLAY0664").GetProperty("changesRouting").GetBoolean().ShouldBeFalse();
    [Fact] void should_require_review_exactly_when_fix_all_is_refused() => Capabilities.GetProperty("actions").EnumerateArray().All(action => action.GetProperty("individualReviewRequired").GetBoolean() == !action.GetProperty("canFixAll").GetBoolean()).ShouldBeTrue();
}
