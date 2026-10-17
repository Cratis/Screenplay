// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_event_routes_golden : Specification
{
    [Fact]
    void should_pin_bytes_revision_and_member_order()
    {
        golden_vector_regeneration.RefuseLegacyEventRoutesRegeneration();
        var bytes = canonical_serialization_golden_vectors.EsmV8Bytes;
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var member in new[] { "eventSources", "sourceKind", "streamKind", "streamIdType", "streamIdParts", "route", "unrouted" })
        {
            text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeTrue();
        }
        foreach (var integer in new[] { "9007199254740991", "9007199254740990", "-9007199254740991", "-9007199254740990" }) text.Contains(integer, StringComparison.Ordinal).ShouldBeTrue();
    }
}
