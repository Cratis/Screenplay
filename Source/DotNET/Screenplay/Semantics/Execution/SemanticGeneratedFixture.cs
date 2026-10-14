// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics.Execution;

internal static partial class SemanticGeneratedFixture
{
    const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    const string Base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static bool Matches(SemanticConcept concept, SemanticValue value, DateTimeOffset? instant = null)
    {
        var generation = concept.Generation ?? new SemanticGeneration("uuid");
        if (generation.Kind == "random")
        {
            return value is SemanticNumberValue number && decimal.Truncate(number.Value) == number.Value &&
                number.Value >= generation.Minimum && number.Value <= generation.Maximum;
        }
        if (value is not SemanticTextValue text) return false;
        if (generation.Kind == "implementation") return true;
        if (generation.Kind == "pattern")
        {
            return GenerationPatternSupport.Matches(GenerationPattern.Parse(generation.Pattern!, generation.Checksum), text.Value, instant);
        }
        var prefix = generation.Prefix ?? string.Empty;
        if (generation.Kind == "typeid" && prefix.Length > 0) prefix += "_";
        if (!text.Value.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var encoded = text.Value[prefix.Length..];
        return generation.Kind switch
        {
            "uuid" or "uuidv7" => Uuid(encoded, generation.Kind == "uuid" ? '4' : '7', generation.Case == "upper"),
            "ulid" => CrockfordValue(encoded, generation.Case != "lower"),
            "typeid" => CrockfordValue(encoded, false) && TypeIdVersion(encoded),
            "nanoid" => encoded.Length == generation.Size && encoded.All((generation.Alphabet ?? SemanticGenerationContract.DefaultAlphabet).Contains),
            "bytes" => Bytes(encoded, generation),
            _ => false
        };
    }

    static bool Uuid(string value, char version, bool upper) =>
        UuidRegex().IsMatch(value) && value[14] == version && "89abAB".Contains(value[19], StringComparison.Ordinal) &&
        value == (upper ? value.ToUpperInvariant() : value.ToLowerInvariant());

    static bool CrockfordValue(string value, bool upper) =>
        value.Length == 26 && value[0] is >= '0' and <= '7' &&
        value.All((upper ? Crockford : Crockford.ToLowerInvariant()).Contains);

    static bool TypeIdVersion(string value)
    {
        var bytes = new byte[16];

        // The first symbol carries three bits, not five: 26 symbols encode 128 bits.
        var bits = 3;
        var buffer = Crockford.IndexOf(char.ToUpperInvariant(value[0]), StringComparison.Ordinal);
        var index = 0;
        foreach (var character in value.Skip(1))
        {
            buffer = (buffer << 5) | Crockford.IndexOf(char.ToUpperInvariant(character), StringComparison.Ordinal);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes[index++] = (byte)(buffer >> bits);
                buffer &= (1 << bits) - 1;
            }
        }

        return (bytes[6] >> 4) == 7 && (bytes[8] & 0xc0) == 0x80;
    }

    static bool Bytes(string value, SemanticGeneration generation)
    {
        var count = generation.Count!.Value;
        return generation.Encoding switch
        {
            "hex" => value.Length == 2 * count && value.All(character => (generation.Case == "upper" ? "0123456789ABCDEF" : "0123456789abcdef").Contains(character, StringComparison.Ordinal)),
            "base64url" => EncodedBytes(value, count, Base64Url, 6),
            "base32" => EncodedBytes(value, count, generation.Case == "lower" ? Base32.ToLowerInvariant() : Base32, 5),
            _ => false
        };
    }

    static bool EncodedBytes(string value, int count, string alphabet, int bitsPerSymbol)
    {
        var symbolCount = ((8 * count) + bitsPerSymbol - 1) / bitsPerSymbol;
        if (value.Length != symbolCount || !value.All(alphabet.Contains)) return false;
        var unused = (symbolCount * bitsPerSymbol) - (8 * count);
        return (alphabet.IndexOf(value[^1], StringComparison.Ordinal) & ((1 << unused) - 1)) == 0;
    }

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex UuidRegex();
}
