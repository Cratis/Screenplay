// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
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
        var previewArguments = new Dictionary<string, object>();
        void Preview(string name, string? value)
        {
            if (value is not null) previewArguments.Add(name, new { value, executable = false });
        }
        Preview("pattern", generation.Pattern);
        Preview("checksum", generation.Checksum);
        Preview("range", generation.Range);
        Preview("rule", generation.Rule);
        var compilation = snapshot.ExecutableCompilation;
        var validationLocations = (concept.Validations ?? []).SelectMany(validation => validation is DeclarativeValidateSyntax declarative
            ? declarative.Rules.Select(rule => rule.Location).Concat((declarative.Requirements ?? []).Select(requirement => requirement.Location))
            : [validation.Location]).ToHashSet();
        var diagnostics = compilation.Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                (diagnostic.Location == generation.Location || diagnostic.Location == concept.Location || validationLocations.Contains(diagnostic.Location)))
            .Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal).ToArray();
        var bits = GenerationEntropy.Bits(generation);
        using var collision = bits is { } known ? JsonDocument.Parse(GenerationEntropy.CollisionCountText(known)) : null;
        var result = new Dictionary<string, object?>
        {
            ["strategy"] = generation.Strategy,
            ["modifiers"] = modifiers,
            ["entropyBits"] = bits,
            ["valuesAtOnePercentCollision"] = collision?.RootElement.Clone(),
            ["executable"] = Admission(compilation, diagnostics)
        };
        if (bits is null) result.Add("entropyUnknownReason", $"Entropy is unknown for generation strategy '{generation.Strategy}' or its authored inputs.");
        if (previewArguments.Count > 0) result.Add("previewArguments", previewArguments);
        McpGenerationRules.Add(result, snapshot, concept);

        return result;
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
