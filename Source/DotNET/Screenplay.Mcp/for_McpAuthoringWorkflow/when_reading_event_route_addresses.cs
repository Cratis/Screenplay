// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_event_route_addresses : Specification
{
    SemanticAddress _source;
    SemanticAddress _stream;

    void Establish()
    {
        _source = SemanticAddress.ForEventSource(ApplicationIdentity.Create("Accounts"), "Account");
        _stream = SemanticAddress.ForEventStream(_source, "Ledger");
    }

    [Fact] void should_round_trip_a_source() => Read(_source).ShouldEqual(_source);
    [Fact] void should_round_trip_an_owned_stream() => Read(_stream).ShouldEqual(_stream);

    static SemanticAddress Read(SemanticAddress address) => McpSemanticAddresses.Read(JsonSerializer.SerializeToElement(
        new { kind = address.Kind.ToString(), parts = address.Parts.Select(part => new { kind = part.Kind.ToString(), part.Key }) },
        McpJson.Options));
}
