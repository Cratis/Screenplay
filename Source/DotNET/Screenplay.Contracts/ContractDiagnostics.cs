// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Contracts;

internal static partial class ContractDiagnostics
{
    // Retirement is documented in diagnostics.md; the constant remains permanently reserved in DiagnosticCodes.
    internal static readonly IReadOnlySet<string> Retired = new HashSet<string>(StringComparer.Ordinal) { DiagnosticCodes.DuplicateReads };

    // Indirect emission parameters and reserved catalog entries have no directly named severity call site.
    // Never default unrecognized codes to error. Adding a new indirect site requires an explicit classification.
    static Dictionary<string, DiagnosticSeverity> Indirect => new(StringComparer.Ordinal)
    {
        [nameof(DiagnosticCodes.DuplicateSpecificationWhen)] = IndirectSeverity("SpecificationParser", "whenAppended"),
        [nameof(DiagnosticCodes.UnclosedPolicyConditionGroup)] = IndirectSeverity("LogicalConditionParser", "diagnostics.UnclosedGroup"),
        [nameof(DiagnosticCodes.InvalidProviderSetting)] = DiagnosticSeverity.Error,
        [nameof(DiagnosticCodes.UnknownReactionTriggerDirective)] = DiagnosticSeverity.Error,
        [nameof(DiagnosticCodes.UnexpectedTokenInCondition)] = IndirectSeverity("LogicalConditionParser", "diagnostics.UnexpectedToken"),
        [nameof(DiagnosticCodes.UnclosedConditionGroup)] = IndirectSeverity("LogicalConditionParser", "diagnostics.UnclosedGroup"),
        [nameof(DiagnosticCodes.DuplicateReads)] = DiagnosticSeverity.Error,
        [nameof(DiagnosticCodes.UnexpectedTokenInAuthorize)] = IndirectSeverity("LogicalConditionParser", "diagnostics.UnexpectedToken"),
        [nameof(DiagnosticCodes.UnclosedAuthorizeGroup)] = IndirectSeverity("LogicalConditionParser", "diagnostics.UnclosedGroup"),
        [nameof(DiagnosticCodes.UnknownQuery)] = IndirectSeverity("ScreenplayValidator", "unknownCode"),
        [nameof(DiagnosticCodes.UnknownScreen)] = IndirectSeverity("ScreenplayValidator", "unknownCode"),
        [nameof(DiagnosticCodes.SemanticMigrationRequired)] = DiagnosticSeverity.Error,
        [nameof(DiagnosticCodes.ProjectionVariantWithoutEntersOn)] = DiagnosticSeverity.Error,
        [nameof(DiagnosticCodes.DuplicateProjectionVariantName)] = DiagnosticSeverity.Error,
        [nameof(DiagnosticCodes.UnknownActionCommand)] = IndirectSeverity("ScreenplayValidator", "unknownCode"),
        [nameof(DiagnosticCodes.UnknownActionScreen)] = IndirectSeverity("ScreenplayValidator", "unknownCode"),
        [nameof(DiagnosticCodes.UnknownActionQuery)] = IndirectSeverity("ScreenplayValidator", "unknownCode"),
        [nameof(DiagnosticCodes.ConflictingSpecificationActions)] = IndirectSeverity("SpecificationParser", "whenAppended"),
        [nameof(DiagnosticCodes.InvalidEventDocumentation)] = IndirectSeverity("DocumentationParser", "code"),
        [nameof(DiagnosticCodes.DuplicateNumericDirective)] = IndirectSeverity("SourceOptionsParser", "code"),
        [nameof(DiagnosticCodes.LateNumericDirective)] = IndirectSeverity("SourceOptionsParser", "code"),
        [nameof(DiagnosticCodes.InvalidDocumentation)] = IndirectSeverity("DocumentationParser", "code")
    };

    internal static JsonArray Create()
    {
        var catalog = ContractSources.Get("compiler/Diagnostics/DiagnosticCodes.cs");
        var result = new JsonArray();
        var titles = CatalogTitles(catalog);
        var severities = EmittedSeverities();
        var indirectSeverities = Indirect;
        foreach (var field in typeof(DiagnosticCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral).OrderBy(field => (string)field.GetRawConstantValue()!, StringComparer.Ordinal))
        {
            var code = (string)field.GetRawConstantValue()!;
            if (!titles.TryGetValue(field.Name, out var title) || title.Length == 0) throw new InvalidScreenplayContract($"{code} lacks a unique catalog title.");
            if (!severities.TryGetValue(field.Name, out var emitted)) emitted = [];
            if (indirectSeverities.TryGetValue(field.Name, out var indirect)) emitted.Add(indirect);
            if (emitted.Count == 0) throw new InvalidScreenplayContract($"{code} lacks a classified severity.");
            if (Retired.Contains(code) && severities.ContainsKey(field.Name)) throw new InvalidScreenplayContract($"Retired code {code} is emitted again.");
            var names = emitted.OrderDescending().Select(severity => severity.ToString().ToLowerInvariant()).ToArray();
            result.Add(new JsonObject
            {
                ["code"] = code,
                ["name"] = field.Name,
                ["severity"] = names[0],
                ["severities"] = new JsonArray([.. names.Select(name => (JsonNode?)JsonValue.Create(name))]),
                ["title"] = title,
                ["retired"] = Retired.Contains(code)
            });
        }

        return result;
    }

    static DiagnosticSeverity IndirectSeverity(string parser, string argument)
    {
        var source = ContractSources.Get($"compiler/Parsing/{parser}.cs");
        var severities = ContractSources.Matches(source, $@"(Error|Warning|Information)\s*\(\s*{Regex.Escape(argument)}\b").Distinct(StringComparer.Ordinal).ToArray();
        if (severities.Length != 1) throw new InvalidScreenplayContract($"Indirect diagnostic severity at {parser}/{argument} is missing or ambiguous.");

        return Enum.Parse<DiagnosticSeverity>(severities[0]);
    }

    static Dictionary<string, string> CatalogTitles(string catalog)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var section in catalog.Split("/// <summary>", StringSplitOptions.None).Skip(1))
        {
            var declaration = CodeDeclarationRegex().Match(section);
            if (!declaration.Success) continue;
            var xml = section[..declaration.Index].Replace("///", string.Empty, StringComparison.Ordinal);
            var element = XElement.Parse($"<summary>{xml}</summary>");
            foreach (var reference in element.Descendants("see")) reference.Value = reference.Attribute("cref")?.Value ?? reference.Value;
            result.Add(declaration.Groups[1].Value, WhitespaceRegex().Replace(element.Value, " ").Trim());
        }

        return result;
    }

    [GeneratedRegex(@"(?:/// )?</summary>\s*public const string (\w+) =", RegexOptions.None, 2000)]
    private static partial Regex CodeDeclarationRegex();

    [GeneratedRegex(@"\s+", RegexOptions.None, 2000)]
    private static partial Regex WhitespaceRegex();

    static Dictionary<string, HashSet<DiagnosticSeverity>> EmittedSeverities()
    {
        var result = new Dictionary<string, HashSet<DiagnosticSeverity>>(StringComparer.Ordinal);
        foreach (var source in ContractSources.All.Where(entry => entry.Key.StartsWith("compiler/", StringComparison.Ordinal) && !entry.Key.EndsWith("DiagnosticCodes.cs", StringComparison.Ordinal)).Select(entry => entry.Value))
        {
            foreach (var pattern in new[] { @"(Error|Warning|Information)\s*\(\s*DiagnosticCodes\.(\w+)", @"DiagnosticSeverity\.(Error|Warning|Information)\s*,\s*DiagnosticCodes\.(\w+)" })
            {
                foreach (Match match in Regex.Matches(source, pattern, RegexOptions.None, TimeSpan.FromSeconds(2)))
                {
                    if (!result.TryGetValue(match.Groups[2].Value, out var values)) result.Add(match.Groups[2].Value, values = []);
                    values.Add(Enum.Parse<DiagnosticSeverity>(match.Groups[1].Value));
                }
            }
        }

        return result;
    }
}
