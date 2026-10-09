// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_comparing_stream_key_schemas : given.a_semantic_comparison
{
    const string Streams = "concept Period : Int\neventsource Account\n  stream Ledger\n    streamId\n      bucket String\n      period Period\n";

    [Fact]
    void should_report_a_schema_order_change_as_potentially_destructive()
    {
        CompareSnapshots(Streams, Streams.Replace("      bucket String\n      period Period", "      period Period\n      bucket String", StringComparison.Ordinal));
        Items("members").Single(item => item.GetProperty("kind").GetString() == "EventStream" && item.GetProperty("member").GetString() == "streamIdParts")
            .GetProperty("contractBreaking").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_not_treat_a_part_name_as_stored_identity()
    {
        CompareSnapshots(Streams, Streams.Replace("bucket String", "partition String", StringComparison.Ordinal));
        Items("members").Single(item => item.GetProperty("kind").GetString() == "EventStream" && item.GetProperty("member").GetString() == "streamIdParts")
            .GetProperty("contractBreaking").ValueKind.ShouldEqual(JsonValueKind.Null);
    }
}
