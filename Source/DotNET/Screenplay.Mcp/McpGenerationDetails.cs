// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

static class McpGenerationDetails
{
    internal static object Summary(object summary, McpSnapshot snapshot, ConceptSyntax concept)
    {
        var details = JsonSerializer.SerializeToNode(summary, McpJson.Options)!.AsObject();
        details.Add("generation", JsonSerializer.SerializeToNode(Read(snapshot, concept), McpJson.Options));

        return details;
    }

    static object Read(McpSnapshot snapshot, ConceptSyntax concept)
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
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GenerationDetails"));
        var documents = SemanticDocumentSet.Create([.. snapshot.Sources.Select(source =>
            SemanticSourceDocument.Create(catalog.ResolveDocument(source.Key), source.Key, source.Key, source.Value))], catalog);
        var diagnostics = new SemanticModelCompiler().Compile("GenerationDetails", documents).Diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                (diagnostic.Location == generation.Location || diagnostic.Location == concept.Location))
            .Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal).ToArray();
        var bits = GenerationEntropy.Bits(generation);
        using var collision = bits is { } known ? JsonDocument.Parse(GenerationEntropy.CollisionCountText(known)) : null;
        var result = new Dictionary<string, object?>
        {
            ["strategy"] = generation.Strategy,
            ["modifiers"] = modifiers,
            ["entropyBits"] = bits,
            ["valuesAtOnePercentCollision"] = collision?.RootElement.Clone(),
            ["executable"] = new { available = diagnostics.Length == 0, diagnosticCodes = diagnostics }
        };
        if (bits is null) result.Add("entropyUnknownReason", $"Entropy is unknown for generation strategy '{generation.Strategy}'.");
        if (previewArguments.Count > 0) result.Add("previewArguments", previewArguments);

        return result;
    }
}
