// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class UiBindingParser
{
    public static UiBindingSyntax ParseFromClause(ParserContext context, string text, SourceLocation location)
    {
        var trimmed = text.Trim();
        var first = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return IsTypedBindingSource(first)
            ? Parse(context, $"from {trimmed}", location)
            : Parse(context, trimmed, location);
    }

    public static UiBindingSyntax Parse(ParserContext context, string text, SourceLocation location)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("from ", StringComparison.Ordinal))
        {
            var legacy = Create(UiBindingKind.DataContext, trimmed, location, trimmed);
            return ParseModifiers(context, legacy, string.Empty, location);
        }

        var body = trimmed["from ".Length..].Trim();
        var head = BindingHeadRegex().Match(body);
        if (!head.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid UI binding '{trimmed}' - expected 'from data <path>', 'from query <Query>[.<path>]' or 'from component <id>.<path>'", location);
            return Create(UiBindingKind.Invalid, body, location, trimmed) with { RawText = trimmed };
        }

        var source = head.Groups[1].Value;
        var expression = head.Groups[2].Value;
        var modifiers = head.Groups[3].Value;
        var binding = source switch
        {
            "data" => Create(UiBindingKind.DataContext, expression, location, trimmed),
            "query" => ParseQuery(context, expression, location, trimmed),
            "component" => ParseComponent(context, expression, location, trimmed),
            _ => Create(UiBindingKind.Invalid, expression, location, trimmed) with { RawText = trimmed }
        };

        return ParseModifiers(context, binding, modifiers, location);
    }

    static bool IsTypedBindingSource(string? source) =>
        string.Equals(source, "data", StringComparison.Ordinal) ||
        string.Equals(source, "query", StringComparison.Ordinal) ||
        string.Equals(source, "component", StringComparison.Ordinal);

    static UiBindingSyntax ParseQuery(ParserContext context, string expression, SourceLocation location, string raw)
    {
        var match = QualifiedPathRegex().Match(expression);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid query binding '{raw}' - expected 'from query <Query>[.<path>]'.", location);
            return Create(UiBindingKind.Invalid, expression, location, raw) with { RawText = raw };
        }

        var query = match.Groups[1].Value;
        var path = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
        return Create(UiBindingKind.QueryResult, path, location, raw) with { Query = query };
    }

    static UiBindingSyntax ParseComponent(ParserContext context, string expression, SourceLocation location, string raw)
    {
        var match = RequiredQualifiedPathRegex().Match(expression);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Invalid component binding '{raw}' - expected 'from component <stableInstanceId>.<outputPath>'.", location);
            return Create(UiBindingKind.Invalid, expression, location, raw) with { RawText = raw };
        }

        var path = match.Groups[2].Value;
        return Create(UiBindingKind.ComponentProperty, path, location, raw) with
        {
            ComponentId = match.Groups[1].Value,
            ComponentPropertyPath = path
        };
    }

    static UiBindingSyntax ParseModifiers(ParserContext context, UiBindingSyntax binding, string modifiers, SourceLocation location)
    {
        var rest = modifiers.Trim();
        while (rest.Length > 0)
        {
            var mode = ModeRegex().Match(rest);
            if (mode.Success)
            {
                binding = binding with { Mode = mode.Groups[1].Value == "twoWay" ? UiBindingMode.TwoWay : UiBindingMode.OneWay };
                rest = mode.Groups[2].Value.Trim();
                continue;
            }

            var nullBehavior = NullRegex().Match(rest);
            if (nullBehavior.Success)
            {
                binding = binding with
                {
                    NullBehavior = nullBehavior.Groups[1].Value switch
                    {
                        "clear" => UiBindingNullBehavior.Clear,
                        "preserve" => UiBindingNullBehavior.Preserve,
                        _ => UiBindingNullBehavior.Propagate
                    }
                };
                rest = nullBehavior.Groups[2].Value.Trim();
                continue;
            }

            var expected = ExpectedRegex().Match(rest);
            if (expected.Success)
            {
                binding = binding with { ExpectedValueType = expected.Groups[1].Value };
                rest = expected.Groups[2].Value.Trim();
                continue;
            }

            context.Error(DiagnosticCodes.UnknownScreenDirective, $"Unsupported UI binding modifier '{rest}' - supported modifiers are 'mode oneWay|twoWay', 'null propagate|clear|preserve' and 'expected <Type>'.", location);
            return binding with { BindingKind = UiBindingKind.Invalid, RawText = binding.RawText ?? rest };
        }

        return binding;
    }

    static UiBindingSyntax Create(UiBindingKind kind, string path, SourceLocation location, string raw) =>
        new(kind, path, location) { RawText = kind == UiBindingKind.Invalid ? raw : null };

    [GeneratedRegex(@"^(data|query|component)\s+(\S+)(.*)$", RegexOptions.None, 1000)]
    private static partial Regex BindingHeadRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)(?:\.(.+))?$", RegexOptions.None, 1000)]
    private static partial Regex QualifiedPathRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\.(.+)$", RegexOptions.None, 1000)]
    private static partial Regex RequiredQualifiedPathRegex();

    [GeneratedRegex(@"^mode\s+(oneWay|twoWay)\b(.*)$", RegexOptions.None, 1000)]
    private static partial Regex ModeRegex();

    [GeneratedRegex(@"^null\s+(propagate|clear|preserve)\b(.*)$", RegexOptions.None, 1000)]
    private static partial Regex NullRegex();

    [GeneratedRegex(@"^expected\s+(\w+(?:\.\w+)*)(.*)$", RegexOptions.None, 1000)]
    private static partial Regex ExpectedRegex();
}
