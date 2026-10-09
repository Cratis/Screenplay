// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides independently droppable scalar, specification-route and composite-route conformance vectors.
/// </summary>
public static class EventRoutesCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.EventRoutes";
    const string Uuid = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    /// <summary>
    /// Gets the scalar command-route vector, including bound-adjacent inputs and literals.
    /// </summary>
    public static CanonicalCorpusVector Scalar => Vector("scalar");

    /// <summary>
    /// Gets the specification-route joining vector.
    /// </summary>
    public static CanonicalCorpusVector Specifications => Vector("specifications");

    /// <summary>
    /// Gets the composite-stream joining vector.
    /// </summary>
    public static CanonicalCorpusVector Composites => Vector("composites");

    /// <summary>
    /// Gets the three vector keys without loading their expected bytes.
    /// </summary>
    public static ImmutableArray<string> Keys => ["scalar", "specifications", "composites"];

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated source forms for a vector.
    /// </summary>
    /// <param name="key">The vector key.</param>
    /// <returns>The source forms with their pinned catalog bytes.</returns>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms(string key)
    {
        var catalog = Resource($"{key}.expected.identity-catalog-folder.json");
        var declarations = Document("declarations", "declarations.play", Resource($"{key}.source.declarations.play"));
        var module = Document("module", "M/module.play", Resource($"{key}.source.module.play"));
        var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = catalog };

        return
        [
            new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = Resource($"{key}.expected.identity-catalog-single.json") },
            folder,
            folder with { Name = "reordered", Documents = [module, declarations] },
            folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
        ];
    }

    /// <summary>
    /// Gets a source-backed refusal with exact diagnostic messages and no publishable artifacts.
    /// </summary>
    /// <param name="key">The rejection fixture key.</param>
    /// <returns>The pinned rejection vector.</returns>
    public static CanonicalCorpusRejectionVector Rejection(string key) => new()
    {
        Name = $"event-routes/rejections/{key}",
        ApplicationName = "EventRoutes",
        ApplicationIdentity = ApplicationIdentity.Create("EventRoutes"),
        SourceForm = RejectionSource(key),
        Diagnostics = JsonSerializer.Deserialize<ImmutableArray<CanonicalCorpusDiagnosticExpectation>>(Resource($"rejections.expected.{key}.json").AsSpan())
    };

    /// <summary>
    /// Gets one rejection source without loading its expected diagnostics.
    /// </summary>
    /// <param name="key">The rejection fixture key.</param>
    /// <returns>The source form.</returns>
    public static CanonicalCorpusSourceForm RejectionSource(string key) => new()
    {
        Name = key,
        Documents = [Document("application", "application.play", Resource($"rejections.{key}.play"))],
        IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("EventRoutes")))]
    };

    static CanonicalCorpusVector Vector(string key) => new()
    {
        Name = $"event-routes/{key}",
        ApplicationName = "EventRoutes",
        ApplicationIdentity = ApplicationIdentity.Create("EventRoutes"),
        RuntimeStreamId = "other",
        SourceForms = SourceForms(key),
        EsmBytes = Resource($"{key}.expected.esm-event-routes.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource($"{key}.expected.semantic-revision.txt").AsSpan()).Trim()),
        SpecificationExpectations = Expectations(key)
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations(string key)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(Resource($"{key}.expected.identity-catalog-folder.json").AsSpan());
        var slice = SemanticAddress.ForSlice(ApplicationIdentity.Create("EventRoutes"), "M", "F", "S");
        var names = key switch
        {
            "scalar" => new[] { "PositiveInput", "PositiveAdjacentInput", "NegativeInput", "NegativeAdjacentInput", "PositiveLiteral", "PositiveAdjacentLiteral", "NegativeLiteral", "NegativeAdjacentLiteral", "TextInput", "UUIDInput", "UnkeyedInput", "EmptyText", "OutsideBound" },
            "specifications" => ["HistoryWithoutProducer", "UnroutedFact"],
            _ => ["CommandParts", "AppendParts", "DifferentPart"]
        };

        return [.. names.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = (name == "EmptyText" || name == "OutsideBound") ? SemanticExecutionOutcomeKind.Rejected : SemanticExecutionOutcomeKind.Accepted,
            RejectionCategory = (name == "EmptyText" || name == "OutsideBound") ? SemanticRejectionCategory.Contract : null,
            RejectionMessage = name switch
            {
                "EmptyText" => "A stream id text literal cannot be empty.",
                "OutsideBound" => "A stream id integer literal must be between -9007199254740991 and 9007199254740991 in Double numeric mode.",
                _ => null
            },
            Passed = name != "DifferentPart",
            WorldFactCount = name switch { "EmptyText" or "OutsideBound" => 0, "HistoryWithoutProducer" or "CommandParts" => 2, _ => 1 },
            Routes = (name == "EmptyText" || name == "OutsideBound") ? [] : [ExpectedRoute(key, name)]
        }).OrderBy(expectation => expectation.Specification.ToString(), StringComparer.Ordinal)];
    }

    static string ExpectedRoute(string key, string name)
    {
        if (name == "UnroutedFact") return "null";
        var source = key == "scalar" ? "StoredAccount" : "Account";
        var stream = name switch { "TextInput" or "HistoryWithoutProducer" => "Notes", "UUIDInput" => "UUIDs", "UnkeyedInput" => "All", _ => key == "scalar" ? "StoredLedger" : "Ledger" };
        var id = name switch
        {
            "TextInput" => "é|%",
            "UUIDInput" => Uuid,
            "HistoryWithoutProducer" => "p-1:2026-10",
            "CommandParts" => $"{Uuid}|a%7Cb%25",
            "AppendParts" => $"{Uuid}|%257C",
            "DifferentPart" => $"{Uuid}|one",
            "UnkeyedInput" => null,
            _ => (name.StartsWith("Negative", StringComparison.Ordinal) ? "-" : string.Empty) + (name.Contains("Adjacent", StringComparison.Ordinal) ? "9007199254740990" : "9007199254740991")
        };

        return id is null
            ? JsonSerializer.Serialize(new { sourceKind = source, streamKind = stream })
            : JsonSerializer.Serialize(new { sourceKind = source, streamKind = stream, streamId = id });
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(EventRoutesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Event-routes corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
