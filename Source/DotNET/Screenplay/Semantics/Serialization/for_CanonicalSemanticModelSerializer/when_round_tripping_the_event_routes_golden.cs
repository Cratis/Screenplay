// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_event_routes_golden : Specification
{
    [Fact]
    void should_pin_bytes_revision_and_member_order()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV8();
        var bytes = SemanticModelSerializer.Serialize(model);
        if (Environment.GetEnvironmentVariable("SCREENPLAY_REGENERATE_EVENT_ROUTES") == "1")
        {
            File.WriteAllBytes(GoldenPath(), bytes);
            throw new GoldenVectorsRegenerated("ESM v8 golden regenerated. Review, rebuild and rerun without SCREENPLAY_REGENERATE_EVENT_ROUTES.");
        }
        bytes.SequenceEqual(canonical_serialization_golden_vectors.EsmV8Bytes).ShouldBeTrue();
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.Revision.ShouldEqual(model.Revision);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var member in new[] { "eventSources", "sourceKind", "streamKind", "streamIdType", "streamIdParts", "route", "unrouted" })
        {
            text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeTrue();
        }
        foreach (var integer in new[] { "9007199254740991", "9007199254740990", "-9007199254740991", "-9007199254740990" }) text.Contains(integer, StringComparison.Ordinal).ShouldBeTrue();
    }

    static string GoldenPath([CallerFilePath] string file = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "..", "Golden", "full-esm-v8.json"));
}
