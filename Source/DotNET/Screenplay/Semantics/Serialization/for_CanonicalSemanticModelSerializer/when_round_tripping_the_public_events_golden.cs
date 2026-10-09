// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_public_events_golden : Specification
{
    [Fact]
    void should_pin_bytes_revision_and_member_order()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV9();
        var bytes = SemanticModelSerializer.Serialize(model);
        bytes.SequenceEqual(canonical_serialization_golden_vectors.EsmV9Bytes).ShouldBeTrue();
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.Revision.ShouldEqual(model.Revision);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var member in new[] { "visibility", "origin", "direction", "sourceEvents" })
        {
            text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeTrue();
        }
    }

    [Fact]
    void should_leave_an_older_model_without_any_public_event_member()
    {
        var text = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV8()));
        foreach (var member in new[] { "visibility", "origin", "direction", "sourceEvents" })
        {
            text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeFalse();
        }
    }
}
