// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal static class GenerationShapes
{
    // Encoding-order alphabets: trailing zero bits constrain the symbol index, not ordinal order.
    const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    const string Base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static IReadOnlyList<GenerationRun>? Runs(SemanticGeneration generation) => generation.Kind switch
    {
        "uuid" or "uuidv7" => Uuid(generation.Kind == "uuid" ? "4" : "7", generation.Case == "upper"),
        "ulid" => [new("01234567", "[0-7]", 1), CrockfordRun(25, generation.Case != "lower")],
        "typeid" => [new("01234567", "[0-7]", 1), CrockfordRun(9, false), new("ef", "[ef]", 1), CrockfordRun(2, false), new("89abrstv", "[89abrstv]", 1), CrockfordRun(12, false)],
        "nanoid" => [new(generation.Alphabet ?? SemanticGenerationContract.DefaultAlphabet, generation.Alphabet is null ? "[A-Za-z0-9_-]" : DerivedPattern.CharacterClass(generation.Alphabet), generation.Size ?? 21)],
        "bytes" when generation.Encoding == "hex" => [Hex(2 * generation.Count!.Value, generation.Case == "upper")],
        "bytes" when generation.Encoding == "base64url" => Encoded(generation.Count!.Value, Base64Url, "[A-Za-z0-9_-]", 6),
        "bytes" when generation.Encoding == "base32" => Encoded(generation.Count!.Value, generation.Case == "lower" ? Base32.ToLowerInvariant() : Base32, generation.Case == "lower" ? "[a-z2-7]" : "[A-Z2-7]", 5),
        _ => null
    };

    static GenerationRun Hex(int count, bool upper) => new(upper ? "0123456789ABCDEF" : "0123456789abcdef", upper ? "[0-9A-F]" : "[0-9a-f]", count);

    static GenerationRun CrockfordRun(int count, bool upper) => new(upper ? Crockford : Crockford.ToLowerInvariant(), upper ? "[0-9A-HJKMNP-TV-Z]" : "[0-9a-hjkmnp-tv-z]", count);

    static GenerationRun[] Uuid(string version, bool upper) =>
    [
        Hex(8, upper), new("-", "-", 1), Hex(4, upper), new("-", "-", 1), new(version, version, 1), Hex(3, upper),
        new("-", "-", 1), new(upper ? "89AB" : "89ab", upper ? "[89AB]" : "[89ab]", 1), Hex(3, upper), new("-", "-", 1), Hex(12, upper)
    ];

    static GenerationRun[] Encoded(int count, string alphabet, string pattern, int bits)
    {
        var length = ((8 * count) + bits - 1) / bits;
        var unused = (length * bits) - (8 * count);
        if (unused == 0) return [new(alphabet, pattern, length)];
        var last = new string([.. alphabet.Where((_, index) => (index & ((1 << unused) - 1)) == 0)]);

        return [new(alphabet, pattern, length - 1), new(last, DerivedPattern.CharacterClass(last), 1)];
    }
}
