// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_StreamIdCodec;

public class when_reading_shared_codec_vectors : Specification
{
    JsonDocument _vectors;

    void Establish()
    {
        using var stream = typeof(when_reading_shared_codec_vectors).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Conformance.stream-id-codec.json")!;
        _vectors = JsonDocument.Parse(stream);
    }

    [Fact]
    void should_format_every_scalar_without_rounding_or_rewriting()
    {
        foreach (var vector in _vectors.RootElement.GetProperty("scalars").EnumerateArray())
        {
            var input = Text(vector);
            string? formatted;
            StreamIdFormatFailure failure;
            var success = vector.GetProperty("kind").GetString() switch
            {
                "String" => SemanticStreamIdFormatter.TryFormatText(input, out formatted, out failure),
                "Uuid" => SemanticStreamIdFormatter.TryFormatUuidText(input, out formatted, out failure),
                _ => BigInteger.TryParse(input, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer)
                    ? SemanticStreamIdFormatter.TryFormatInteger(integer, vector.GetProperty("doubleBound").GetBoolean(), out formatted, out failure)
                    : SemanticStreamIdFormatter.TryFormatInteger(double.Parse(input, CultureInfo.InvariantCulture), out formatted, out failure)
            };
            Check(vector, success, formatted, failure);
        }
    }

    [Fact]
    void should_encode_decode_and_reencode_every_composite_in_declared_order()
    {
        foreach (var vector in _vectors.RootElement.GetProperty("composites").EnumerateArray())
        {
            var parts = vector.GetProperty("parts").EnumerateArray().Select(part => part.GetString()!).ToArray();
            var encoded = SemanticStreamIdFormatter.EncodeComposite(parts);
            encoded.ShouldEqual(vector.GetProperty("encoded").GetString());
            SemanticStreamIdFormatter.TryDecodeComposite(encoded, Kinds(vector), out var decoded, out var failure).ShouldBeTrue();
            failure.ShouldEqual(StreamIdFormatFailure.None);
            decoded!.SequenceEqual(parts).ShouldBeTrue();
            SemanticStreamIdFormatter.EncodeComposite(decoded).ShouldEqual(encoded);
        }
    }

    [Fact]
    void should_refuse_every_malformed_encoded_identity_without_repair()
    {
        foreach (var vector in _vectors.RootElement.GetProperty("decode").EnumerateArray())
        {
            var success = SemanticStreamIdFormatter.TryDecodeComposite(Text(vector), Kinds(vector), out var parts, out var failure);
            Check(vector, success, null, failure);
            Assert.Null(parts);
        }
    }

    [Fact]
    void should_pin_every_value_free_failure_message()
    {
        foreach (var message in _vectors.RootElement.GetProperty("messages").EnumerateObject())
        {
            SemanticStreamIdFormatter.FailureMessage(Enum.Parse<StreamIdFormatFailure>(message.Name)).ShouldEqual(message.Value.GetString());
        }
    }

    static void Check(JsonElement vector, bool success, string? formatted, StreamIdFormatFailure failure)
    {
        if (vector.TryGetProperty("failure", out var expected))
        {
            success.ShouldBeFalse();
            failure.ShouldEqual(Enum.Parse<StreamIdFormatFailure>(expected.GetString()!));
            Assert.Null(formatted);
        }
        else
        {
            success.ShouldBeTrue();
            failure.ShouldEqual(StreamIdFormatFailure.None);
            formatted.ShouldEqual(vector.GetProperty("canonical").GetString());
        }
    }

    static string Text(JsonElement vector) => vector.TryGetProperty("utf16", out var units)
        ? new string([.. units.EnumerateArray().Select(unit => (char)int.Parse(unit.GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture))])
        : vector.GetProperty("input").GetString()!;

    static StreamIdScalarKind[] Kinds(JsonElement vector) => [.. vector.GetProperty("kinds").EnumerateArray().Select(kind => kind.GetString() switch
    {
        "String" => StreamIdScalarKind.Text,
        "Uuid" => StreamIdScalarKind.Uuid,
        _ => StreamIdScalarKind.WholeNumber
    })];
}
