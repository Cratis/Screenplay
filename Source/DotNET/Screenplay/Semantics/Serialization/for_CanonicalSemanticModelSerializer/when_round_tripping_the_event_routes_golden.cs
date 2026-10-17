// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_event_routes_golden : Specification
{
    [Fact]
    void should_preserve_event_route_members_in_the_feature_corpus()
    {
        var texts = new List<string>();
        foreach (var key in new[] { "scalar", "specifications", "composites", "composite-commands" })
        {
            var bytes = feature_corpus_vectors.Read($"EventRoutes/{key}/expected/esm-v8.json");
            var model = SemanticModelSerializer.Deserialize(bytes);
            SemanticModelSerializer.Serialize(model).SequenceEqual(bytes).ShouldBeTrue();
            texts.Add(Encoding.UTF8.GetString(bytes));
        }
        var text = string.Join('\n', texts);
        foreach (var member in new[] { "eventSources", "sourceKind", "streamKind", "streamIdType", "streamIdParts", "route", "unrouted" })
        {
            text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeTrue();
        }
        foreach (var integer in new[] { "9007199254740991", "9007199254740990", "-9007199254740991", "-9007199254740990" }) text.Contains(integer, StringComparison.Ordinal).ShouldBeTrue();
    }

}
