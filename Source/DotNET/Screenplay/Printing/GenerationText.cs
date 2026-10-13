// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

internal static class GenerationText
{
    internal static string Format(GenerationSyntax generation)
    {
        var valid = generation.Strategy switch
        {
            "uuid" or "uuidv7" or "ulid" or "nanoid" or "sequence" => true,
            "typeid" => generation.Prefix is not null,
            "bytes" => generation.Count is not null && generation.Encoding is not null,
            "pattern" => generation.Pattern is not null,
            "random" => generation.Range is not null,
            "rule" => generation.Rule is not null,
            _ => false
        };
        if (!valid) throw new UnsupportedSyntaxForPrinting("generation strategy", generation.Strategy);
        var parts = new List<string> { "generate", generation.Strategy };
        switch (generation.Strategy)
        {
            case "typeid": parts.Add(StringLiteral.Quote(generation.Prefix!)); break;
            case "nanoid" when generation.Size is { } size: parts.Add(size.ToString(CultureInfo.InvariantCulture)); break;
            case "bytes":
                parts.Add(generation.Count!.Value.ToString(CultureInfo.InvariantCulture));
                parts.Add(generation.Encoding!);
                break;
            case "pattern": parts.Add(StringLiteral.Quote(generation.Pattern!)); break;
            case "random": parts.Add(generation.Range!); break;
            case "rule": parts.Add(generation.Rule!); break;
        }

        if (generation.Alphabet is { } alphabet) parts.Add($"alphabet {StringLiteral.Quote(alphabet)}");
        if (generation.Prefix is { } prefix && generation.Strategy != "typeid") parts.Add($"prefix {StringLiteral.Quote(prefix)}");
        if (generation.Case is { } letterCase) parts.Add($"case {letterCase}");
        if (generation.Checksum is { } checksum) parts.Add($"checksum {checksum}");
        return string.Join(' ', parts);
    }
}
