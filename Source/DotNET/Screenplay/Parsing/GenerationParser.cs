// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class GenerationParser
{
    internal static GenerationSyntax? Parse(ParserContext context, SourceLine line, string primitive)
    {
        var tokens = TokenRegex().Matches(line.Content).Select(match => match.Value).Skip(1).ToArray();
        if (tokens.Length == 0)
        {
            context.Error(DiagnosticCodes.InvalidGenerationArgument, "Expected 'generate <strategy> [modifiers]'.", line.Location);
            return null;
        }

        var strategy = tokens[0];
        if (!OneOf(strategy, "uuid", "uuidv7", "ulid", "typeid", "nanoid", "bytes", "pattern", "random", "rule", "sequence"))
        {
            context.Error(DiagnosticCodes.UnknownGenerationStrategy, $"Unknown generation strategy '{strategy}'.", line.Location);
            return null;
        }

        var (generation, next, valid) = Arguments(new(strategy, line.Location), tokens);
        if (!valid)
        {
            context.Error(DiagnosticCodes.InvalidGenerationArgument, $"Missing or invalid argument for generation strategy '{strategy}'.", line.Location);
            return generation;
        }

        var result = Modifiers(context, line, primitive, generation, tokens, next);
        Validate(context, line, primitive, result);
        return ParseBody(context, line, result);
    }

    static (GenerationSyntax Generation, int Next, bool Valid) Arguments(GenerationSyntax source, string[] tokens)
    {
        var argument = tokens.ElementAtOrDefault(1);
        var literal = argument is null ? null : Literal(argument);
        var number = argument is null ? null : Integer(argument);
        var encoding = tokens.ElementAtOrDefault(2);
        return source.Strategy switch
        {
            "typeid" => (source with { Prefix = literal }, 2, literal is not null),
            "pattern" => (source with { Pattern = literal }, 2, literal is not null),
            "nanoid" when number is not null => (source with { Size = number }, 2, true),
            "bytes" => (source with { Count = number, Encoding = encoding }, 3, number is not null && OneOf(encoding, "base64url", "hex", "base32")),
            "random" => (source with { Range = argument }, 2, argument is not null && RangeRegex().IsMatch(argument) && ValidRange(argument)),
            "rule" => (source with { Rule = argument }, 2, argument is not null && NameRegex().IsMatch(argument)),
            _ => (source, 1, true)
        };
    }

    static GenerationSyntax Modifiers(ParserContext context, SourceLine line, string primitive, GenerationSyntax result, string[] tokens, int index)
    {
        var strategy = result.Strategy;
        var seen = new HashSet<string>();
        while (index < tokens.Length)
        {
            var modifier = tokens[index++];
            var value = index < tokens.Length ? tokens[index++] : null;
            if (!seen.Add(modifier)) context.Error(DiagnosticCodes.DuplicateGenerationModifier, $"Generation modifier '{modifier}' is repeated.", line.Location);
            if (!Allows(strategy, primitive, result.Encoding, modifier))
            {
                context.Error(DiagnosticCodes.InvalidGenerationModifier, $"Generation modifier '{modifier}' is not allowed for '{strategy}' on '{primitive}'.", line.Location);
                continue;
            }

            var text = value is null ? null : Literal(value);
            var validValue = modifier switch
            {
                "prefix" or "alphabet" => text is not null,
                "case" => OneOf(value, "lower", "upper"),
                _ => OneOf(value, "luhn", "mod11", "mod97")
            };
            if (!validValue)
            {
                context.Error(DiagnosticCodes.InvalidGenerationArgument, $"Missing or invalid value for generation modifier '{modifier}'.", line.Location);
                continue;
            }

            result = modifier switch
            {
                "prefix" => result with { Prefix = text },
                "alphabet" => result with { Alphabet = text },
                "case" => result with { Case = value },
                _ => result with { Checksum = value }
            };
        }

        return result;
    }

    static void Validate(ParserContext context, SourceLine line, string primitive, GenerationSyntax generation)
    {
        var strategy = generation.Strategy;
        if (strategy == "pattern" && generation.Pattern is { } pattern)
        {
            try
            {
                _ = GenerationPattern.Parse(pattern, generation.Checksum);
            }
            catch (InvalidGenerationPattern error)
            {
                var code = error.Message.Contains("expanded length", StringComparison.Ordinal) ? DiagnosticCodes.GenerationSizeOutsideRange : DiagnosticCodes.InvalidGenerationArgument;
                context.Error(code, error.Message, line.Location);
            }
        }
        var allowed = strategy == "sequence" || primitive switch
        {
            "Uuid" => OneOf(strategy, "uuid", "uuidv7"),
            "String" => strategy != "random",
            "Int" => strategy == "random",
            _ => false
        };
        if (!allowed) context.Error(DiagnosticCodes.InvalidGenerationPrimitive, $"Generation strategy '{strategy}' is not allowed on '{primitive}'.", line.Location);
        if (generation.Prefix is { } prefix)
        {
            if (strategy == "typeid")
            {
                if (!TypeIdPrefixRegex().IsMatch(prefix)) context.Error(DiagnosticCodes.InvalidTypeIdPrefix, "TypeID prefix must be empty or 1..63 lowercase letters and underscores, starting and ending with a letter.", line.Location);
            }
            else if (prefix.Length > 32 || !Printable(prefix))
            {
                context.Error(DiagnosticCodes.InvalidGenerationPrefix, "Generation prefix must contain at most 32 printable ASCII characters.", line.Location);
            }
        }

        if (generation.Alphabet is { } alphabet && (alphabet.Length is < 2 or > 256 || !Printable(alphabet) || alphabet.Distinct().Count() != alphabet.Length))
        {
            context.Error(DiagnosticCodes.InvalidGenerationAlphabet, "NanoID alphabet must contain 2..256 distinct printable ASCII characters.", line.Location);
        }

        if (generation.Size is < 1 or > 256 || generation.Count is < 1 or > 512)
        {
            context.Error(DiagnosticCodes.GenerationSizeOutsideRange, "NanoID size must be 1..256 and bytes count must be 1..512.", line.Location);
        }
    }

    static bool Allows(string strategy, string primitive, string? encoding, string modifier) => modifier switch
    {
        "prefix" => primitive == "String" && OneOf(strategy, "uuid", "uuidv7", "ulid", "nanoid", "bytes"),
        "alphabet" => strategy == "nanoid",
        "case" => strategy == "ulid" || (primitive == "String" && OneOf(strategy, "uuid", "uuidv7")) || (strategy == "bytes" && OneOf(encoding, "hex", "base32")),
        "checksum" => strategy == "pattern",
        _ => false
    };

    static bool OneOf(string? value, params string[] choices) => choices.Contains(value, StringComparer.Ordinal);

    static bool Printable(string text) => text.All(character => character is >= ' ' and <= '~');
    static int? Integer(string text) => !text.StartsWith('+') && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : null;
    static string? Literal(string text) => text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? StringLiteral.Unescape(text[1..^1]) : null;
    static bool ValidRange(string text)
    {
        var parts = text.Split("..", StringSplitOptions.None);
        return long.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var minimum) &&
            long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var maximum) && minimum <= maximum;
    }

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|\\S+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)\.\.-?(0|[1-9][0-9]*)\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex RangeRegex();

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"^([a-z]([a-z_]{0,61}[a-z])?)?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TypeIdPrefixRegex();
}
