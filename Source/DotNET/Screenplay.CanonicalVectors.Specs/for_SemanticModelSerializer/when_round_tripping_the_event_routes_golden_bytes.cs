// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_event_routes_golden_bytes : Specification
{
    [Fact]
    void should_preserve_the_exact_bytes_on_every_supported_runtime()
    {
        using var stream = typeof(when_round_tripping_the_event_routes_golden_bytes).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Golden.event-routes-esm.json")!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var expected = bytes.ToArray();
        var model = SemanticModelSerializer.Deserialize(expected);
        model.LanguageVersion.ShouldEqual(EventRoutesVersion.Language);
        model.SemanticVersion.ShouldEqual(EventRoutesVersion.Semantic);
        SemanticModelSerializer.Serialize(model).SequenceEqual(expected).ShouldBeTrue();
        model.Application.EventSources.ShouldNotBeEmpty();
        model.Application.EventSources.SelectMany(source => source.Streams).Any(stream => stream.StreamIdParts.Length >= 2).ShouldBeTrue();
    }
}
