// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_public_events_golden_bytes : Specification
{
    [Fact]
    void should_preserve_the_exact_bytes_on_every_supported_runtime()
    {
        using var stream = typeof(when_round_tripping_the_public_events_golden_bytes).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Golden.full-esm-v9.json")!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var expected = bytes.ToArray();
        var model = SemanticModelSerializer.Deserialize(expected);
        model.LanguageVersion.ShouldEqual(LanguageVersion.V9);
        model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
        SemanticModelSerializer.Serialize(model).SequenceEqual(expected).ShouldBeTrue();
        var slices = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).ToArray();
        slices.Any(slice => slice.Direction == SemanticTranslationDirection.Outbound && slice.Projections.Any(projection => projection.Target == SemanticProjectionTargetKind.Event)).ShouldBeTrue();
        slices.Any(slice => slice.Captures.Any(capture => capture.EventsSource is not null)).ShouldBeTrue();
        slices.SelectMany(slice => slice.Events).Any(@event => @event.Origin is not null).ShouldBeTrue();
    }
}
