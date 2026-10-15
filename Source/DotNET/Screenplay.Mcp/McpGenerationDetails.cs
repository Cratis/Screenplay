// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

static class McpGenerationDetails
{
    internal static object WithGeneration(object details, string view, object syntax, McpSnapshot snapshot) =>
        view == "summary" && syntax is ConceptSyntax { Generation: not null } concept ? Summary(details, snapshot, concept) : details;

    static System.Text.Json.Nodes.JsonObject Summary(object summary, McpSnapshot snapshot, ConceptSyntax concept)
    {
        var details = JsonSerializer.SerializeToNode(summary, McpJson.Options)!.AsObject();
        details.Add("generation", JsonSerializer.SerializeToNode(Read(snapshot, concept), McpJson.Options));

        return details;
    }

    static Dictionary<string, object?> Read(McpSnapshot snapshot, ConceptSyntax concept)
    {
        var generation = concept.Generation!;
        var modifiers = new Dictionary<string, object>();
        void Add(string name, object? value)
        {
            if (value is not null) modifiers.Add(name, value);
        }
        Add("prefix", generation.Prefix);
        Add("case", generation.Case);
        Add("size", generation.Size);
        Add("alphabet", generation.Alphabet);
        Add("count", generation.Count);
        Add("encoding", generation.Encoding);
        var compilation = snapshot.ExecutableCompilation;
        var validationLocations = (concept.Validations ?? []).SelectMany(validation => validation is DeclarativeValidateSyntax declarative
            ? declarative.Rules.Select(rule => rule.Location).Concat((declarative.Requirements ?? []).Select(requirement => requirement.Location))
            : [validation.Location]).ToHashSet();
        var diagnostics = compilation.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                (diagnostic.Location == generation.Location || diagnostic.Location == concept.Location || validationLocations.Contains(diagnostic.Location)))
            .Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal).ToArray();
        var result = new Dictionary<string, object?>
        {
            ["strategy"] = generation.Strategy,
            ["modifiers"] = modifiers,
            ["entropyBits"] = null,
            ["valuesAtOnePercentCollision"] = null,
            ["executable"] = Admission(compilation, diagnostics)
        };
        AddEntropy(result, generation);
        var arguments = Arguments(generation);
        if (arguments.Count > 0) result.Add("arguments", arguments);
        McpGenerationRules.Add(result, snapshot, concept);

        return result;
    }

    static Dictionary<string, object> Arguments(GenerationSyntax generation)
    {
        var arguments = new Dictionary<string, object>();
        if (generation.Pattern is { } pattern) arguments.Add("pattern", pattern);
        if (generation.Checksum is { } checksum) arguments.Add("checksum", checksum);
        if (generation.Range is { } range) arguments.Add("range", Range(range));
        if (generation.Rule is { } name) arguments.Add("rule", name);

        return arguments;
    }

    static object Range(string text)
    {
        try
        {
            var range = GenerationIntegerRange.Parse(text);

            return new { text, minimum = range.Minimum, maximum = range.Maximum };
        }
        catch (InvalidSemanticContract)
        {
            return new { text };
        }
    }

    static void AddEntropy(Dictionary<string, object?> result, GenerationSyntax generation)
    {
        var bits = GenerationEntropy.Bits(generation);
        var reason = generation.Strategy == "rule"
            ? "Entropy is unknown because the generation rule implementation is opaque."
            : $"Entropy is unknown for generation strategy '{generation.Strategy}' or its authored inputs.";
        try
        {
            if (bits is null && GenerationEntropy.ExactSupportCount(generation, DateTimeOffset.UtcNow.Year) is { } count)
            {
                bits = GenerationEntropy.SupportBits(count);
                if (generation.Strategy is "pattern" or "random") result.Add("supportCount", Number(count.ToString(CultureInfo.InvariantCulture)));
            }
        }
        catch (Exception error) when (error is InvalidGenerationPattern or GenerationSupportBudgetExceeded or EmptyGenerationSupport or InvalidSemanticContract)
        {
            reason = error.Message;
        }
        result["entropyBits"] = bits;
        result["valuesAtOnePercentCollision"] = bits is { } known ? Number(GenerationEntropy.CollisionCountText(known)) : null;
        if (bits is null) result.Add("entropyUnknownReason", reason);
    }

    static JsonElement Number(string text)
    {
        using var document = JsonDocument.Parse(text);

        return document.RootElement.Clone();
    }

    static Dictionary<string, object> Admission(CompilationResult<SemanticCompilation> compilation, string[] diagnostics)
    {
        var status = (diagnostics.Length > 0, compilation.Success) switch
        {
            (true, _) => "refused",
            (false, true) => "admitted",
            _ => "notAssessed"
        };
        var result = new Dictionary<string, object>
        {
            ["status"] = status,
            ["diagnosticCodes"] = diagnostics
        };
        if (status == "notAssessed") result.Add("reason", "The whole model did not produce an executable model; generation admission was not established. See executable diagnostics.");

        return result;
    }
}
