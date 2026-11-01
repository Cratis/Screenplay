// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Contexts;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReducerContextKey;

public class when_reading_shared_uuid_and_text_vectors : Specification
{
    JsonElement[] _uuids;
    string[] _texts;
    readonly List<string> _differences = [];

    void Establish()
    {
        using var stream = typeof(when_reading_shared_uuid_and_text_vectors).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Conformance.stream-id-codec.json")!;
        using var document = JsonDocument.Parse(stream);
        var scalars = document.RootElement.GetProperty("scalars").EnumerateArray().ToArray();
        _uuids = [.. scalars.Where(vector => vector.GetProperty("kind").GetString() == "Uuid").Select(vector => vector.Clone())];
        _texts = [.. scalars.Where(vector => vector.GetProperty("kind").GetString() == "String").Select(Text), "a|b", "%7C", "%257C"];
    }

    void Because()
    {
        foreach (var vector in _uuids)
        {
            var context = Context(Text(vector));
            var formatted = SemanticStreamIdFormatter.TryFormatUuidText(context.Key, out var canonical, out _);
            if (vector.TryGetProperty("failure", out _))
            {
                if (formatted || Catch.Exception(() => context.KeyAs<Guid>()) is not ContextKeyNotConvertible
                    || Catch.Exception(() => context.KeyAs<given.UuidKey>()) is not ContextKeyNotConvertible)
                {
                    _differences.Add(vector.GetProperty("id").GetString()!);
                }
            }
            else if (!formatted || canonical != vector.GetProperty("canonical").GetString()
                || context.KeyAs<Guid>() != Guid.Parse(canonical!)
                || context.KeyAs<given.UuidKey>().Value != Guid.Parse(canonical!))
            {
                _differences.Add(vector.GetProperty("id").GetString()!);
            }
        }
    }

    [Fact] void should_agree_with_every_uuid_vector_and_the_codec() => _differences.ShouldBeEmpty();

    [Fact]
    void should_pass_every_text_vector_through_without_codec_normalization()
    {
        foreach (var text in _texts)
        {
            var context = Context(text);
            context.KeyAs<string>().ShouldEqual(text);
            context.KeyAs<given.TextKey>().Value.ShouldEqual(text);
        }
    }

    static ReducerContext Context(string key) => new(null, 0, key, TenantId.Default, DateTimeOffset.UnixEpoch, 0);

    static string Text(JsonElement vector) => vector.TryGetProperty("utf16", out var units)
        ? new string([.. units.EnumerateArray().Select(unit => (char)int.Parse(unit.GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture))])
        : vector.GetProperty("input").GetString()!;
}
