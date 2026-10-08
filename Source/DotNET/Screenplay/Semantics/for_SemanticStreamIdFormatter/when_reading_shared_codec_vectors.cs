// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.for_SemanticStreamIdFormatter;

public class when_reading_shared_codec_vectors : Specification
{
    [Fact]
    void should_hold_the_portable_codec_to_shared_vectors_under_noninvariant_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            var vectors = Vectors();
            foreach (var vector in vectors.GetProperty("scalars").EnumerateArray())
            {
                var input = Input(vector);
                var kind = Kind(vector.GetProperty("kind").GetString()!);
                var expected = vector.TryGetProperty("canonical", out var canonical) ? canonical.GetString() : null;
                var expectedFailure = vector.TryGetProperty("failure", out var refused) ? Enum.Parse<StreamIdFormatFailure>(refused.GetString()!) : StreamIdFormatFailure.None;
                string? formatted;
                StreamIdFormatFailure failure;
                var valid = kind switch
                {
                    StreamIdScalarKind.Text => SemanticStreamIdFormatter.TryFormatText(input, out formatted, out failure),
                    StreamIdScalarKind.Uuid => SemanticStreamIdFormatter.TryFormatUuidText(input, out formatted, out failure),
                    _ => input.Contains('.') ? SemanticStreamIdFormatter.TryFormatInteger(double.Parse(input, CultureInfo.InvariantCulture), out formatted, out failure)
                        : SemanticStreamIdFormatter.TryFormatInteger(BigInteger.Parse(input, CultureInfo.InvariantCulture), vector.GetProperty("doubleBound").GetBoolean(), out formatted, out failure)
                };
                valid.ShouldEqual(expected is not null);
                formatted.ShouldEqual(expected);
                failure.ShouldEqual(expectedFailure);
                if (kind == StreamIdScalarKind.WholeNumber && vector.GetProperty("doubleBound").GetBoolean())
                {
                    SemanticStreamIdFormatter.TryFormatInteger(double.Parse(input, CultureInfo.InvariantCulture), out formatted, out failure).ShouldEqual(valid);
                    formatted.ShouldEqual(expected);
                    failure.ShouldEqual(expectedFailure);
                }
            }
            foreach (var vector in vectors.GetProperty("composites").EnumerateArray())
            {
                var parts = vector.GetProperty("parts").EnumerateArray().Select(value => value.GetString()!).ToArray();
                var encoded = vector.GetProperty("encoded").GetString()!;
                SemanticStreamIdFormatter.EncodeComposite(parts).ShouldEqual(encoded);
                SemanticStreamIdFormatter.TryDecodeComposite(encoded, Kinds(vector), out var decoded, out var failure).ShouldBeTrue();
                decoded!.ToArray().ShouldEqual(parts);
                failure.ShouldEqual(StreamIdFormatFailure.None);
                SemanticStreamIdFormatter.EncodeComposite(decoded!).ShouldEqual(encoded);
            }
            foreach (var vector in vectors.GetProperty("decode").EnumerateArray())
            {
                SemanticStreamIdFormatter.TryDecodeComposite(Input(vector), Kinds(vector), out var parts, out var failure).ShouldBeFalse();
                parts.ShouldBeNull();
                failure.ShouldEqual(Enum.Parse<StreamIdFormatFailure>(vector.GetProperty("failure").GetString()!));
            }
            foreach (var message in vectors.GetProperty("messages").EnumerateObject()) SemanticStreamIdFormatter.FailureMessage(Enum.Parse<StreamIdFormatFailure>(message.Name)).ShouldEqual(message.Value.GetString());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    void should_allow_exact_mode_decoding_and_refuse_nonfinite_integers()
    {
        SemanticStreamIdFormatter.TryDecodeComposite("9007199254740993|b", [StreamIdScalarKind.WholeNumber, StreamIdScalarKind.Text], out var parts, out _, false).ShouldBeTrue();
        parts![0].ShouldEqual("9007199254740993");
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            SemanticStreamIdFormatter.TryFormatInteger(value, out _, out var failure).ShouldBeFalse();
            failure.ShouldEqual(StreamIdFormatFailure.NotIntegral);
        }
    }

    static string Input(JsonElement vector) => vector.TryGetProperty("input", out var value) ? value.GetString()!
        : new string(vector.GetProperty("utf16").EnumerateArray().Select(unit => (char)int.Parse(unit.GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray());
    static StreamIdScalarKind Kind(string kind) => kind switch { "String" => StreamIdScalarKind.Text, "Uuid" => StreamIdScalarKind.Uuid, _ => StreamIdScalarKind.WholeNumber };
    static StreamIdScalarKind[] Kinds(JsonElement vector) => vector.GetProperty("kinds").EnumerateArray().Select(value => Kind(value.GetString()!)).ToArray();

    static JsonElement Vectors([CallerFilePath] string path = "")
    {
        var root = Directory.GetParent(path);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Source", "Screenplay", "Compiler", "Conformance", "stream-id-codec.json")));

        return document.RootElement.Clone();
    }
}
