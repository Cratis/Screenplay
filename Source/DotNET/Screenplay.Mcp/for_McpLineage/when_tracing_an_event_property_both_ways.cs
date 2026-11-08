// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_tracing_an_event_property_both_ways : given.a_lineage_model
{
    JsonElement _report;
    JsonElement[] _hops = [];
    JsonElement[] _upstream = [];

    void Establish() => Write("""
        policy ClaimsAutomation
          require role "ClaimsAutomation"

        module Claims
          feature Expiration
            slice Automation CloseExpiredClaims
              event ClaimDeadlinePassed
                claim String
              command CloseClaim
                claim String
                authorize ClaimsAutomation
                produces ClaimClosed
                  claim = claim
              event ClaimClosed
                claim String
              reaction CloseExpiredClaims
                runs as system role "ClaimsAutomation"
                when ClaimDeadlinePassed
                  claim
                  invokes CloseClaim
                    claim = claim
        """);

    void Because()
    {
        _report = Trace("Claims.Expiration.CloseExpiredClaims.ClaimDeadlinePassed.claim");
        _hops = Hops(_report);
        _upstream = Hops(Call("trace-lineage", new { address = "Claims.Expiration.CloseExpiredClaims.ClaimDeadlinePassed.claim", direction = "upstream" }).GetProperty("result").GetProperty("structuredContent"));
    }

    [Fact] void should_follow_the_reaction_invocation_into_the_command_and_its_event() => Chain(_hops, _hops.Last(hop => hop.GetProperty("direction").GetString() == "downstream")).ShouldEqual(["event-property", "invocation-mapping", "command-property", "production-mapping", "event-property"]);
    [Fact] void should_end_downstream_at_the_produced_event_property() => Text(_hops.Last(hop => hop.GetProperty("direction").GetString() == "downstream"), "address").ShouldEqual("Claims.Expiration.CloseExpiredClaims.ClaimClosed.claim");
    [Fact] void should_report_that_nothing_produces_the_trigger_event() => _hops.Single(hop => hop.GetProperty("direction").GetString() == "upstream").GetProperty("status").GetString().ShouldEqual("missing");
    [Fact] void should_report_the_trace_as_incomplete() => _report.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_go_only_upstream_when_asked() => _upstream.Select(hop => hop.GetProperty("direction").GetString()).ShouldEqual(["subject", "upstream"]);
}
