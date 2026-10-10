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
    public static CanonicalCorpusVector V8 => Vector("scalar");

    /// <summary>
    /// Gets the specification-route joining vector.
    /// </summary>
    public static CanonicalCorpusVector SpecificationsV8 => Vector("specifications");

    /// <summary>
    /// Gets the composite specification vector, requiring both joining parts.
    /// </summary>
    public static CanonicalCorpusVector CompositesV8 => Vector("composites");

    /// <summary>
    /// Gets composite command routes and outcomes without depending on specification-route admission.
    /// </summary>
    public static CanonicalCorpusVector CompositeCommandsV8 => Vector("composite-commands");

    /// <summary>
    /// Gets the vector keys without loading their expected bytes.
    /// </summary>
    public static ImmutableArray<string> Keys => ["scalar", "specifications", "composites", "composite-commands", "production-routes", "observer-filters"];

    /// <summary>
    /// Gets complete per-production route replacement and eager route failure vectors.
    /// </summary>
    public static CanonicalCorpusVector ProductionRoutesV10 => Vector("production-routes");

    /// <summary>
    /// Gets source and stream filter vectors for reactions and reducers.
    /// </summary>
    public static CanonicalCorpusVector ObserverFiltersV10 => Vector("observer-filters");

    /// <summary>
    /// Gets the admitted schema version for a corpus key.
    /// </summary>
    /// <param name="key">The vector key.</param>
    /// <returns>The schema version.</returns>
    public static int Version(string key) => (key == "production-routes" || key == "observer-filters") ? 10 : 8;

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
        Name = $"event-routes/v8/rejections/{key}",
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
        Name = $"event-routes/v{Version(key)}/{key}",
        ApplicationName = "EventRoutes",
        ApplicationIdentity = ApplicationIdentity.Create("EventRoutes"),
        RuntimeStreamId = "other",
        SourceForms = SourceForms(key),
        EsmBytes = Resource($"{key}.expected.esm-v{Version(key)}.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource($"{key}.expected.semantic-revision.txt").AsSpan()).Trim()),
        SpecificationExpectations = Expectations(key)
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations(string key)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(Resource($"{key}.expected.identity-catalog-folder.json").AsSpan());
        var slice = SemanticAddress.ForSlice(ApplicationIdentity.Create("EventRoutes"), "M", "F", "S");
        string[] names = key switch
        {
            "production-routes" => ["Override", "NoDefault", "SkippedRouteFailure"],
            "observer-filters" => ["RoutedReaction", "UnroutedReaction", "UnroutedReducer", "MatchingReducer"],
            "scalar" => ["PositiveInput", "PositiveAdjacentInput", "NegativeInput", "NegativeAdjacentInput", "PositiveLiteral", "PositiveAdjacentLiteral", "NegativeLiteral", "NegativeAdjacentLiteral", "TextInput", "UUIDInput", "UnkeyedInput", "EmptyText", "OutsideBound", "UnauthorizedRouteFailure", "ValidationRouteFailure"],
            "specifications" => ["HistoryWithoutProducer", "UnroutedFact", "AnyOrderAssignment"],
            "composite-commands" => ["CommandOnlyParts", "CommandOnlyOpaqueEscape"],
            _ => ["CommandParts", "AppendParts", "DifferentPart"]
        };

        return [.. names.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = name switch
            {
                "MatchingReducer" => SemanticExecutionOutcomeKind.Unsupported,
                _ when RejectionCategory(name) is not null => SemanticExecutionOutcomeKind.Rejected,
                _ => SemanticExecutionOutcomeKind.Accepted
            },
            UnsupportedCapability = name == "MatchingReducer" ? SemanticExecutionCapability.Projection : null,
            RejectionCategory = RejectionCategory(name),
            RejectionMessage = name switch
            {
                "EmptyText" or "SkippedRouteFailure" => "A stream id text literal cannot be empty.",
                "OutsideBound" => "A stream id integer literal must be between -9007199254740991 and 9007199254740991 in Double numeric mode.",
                "UnauthorizedRouteFailure" => "Caller is not authorized.",
                "ValidationRouteFailure" => "Text is required.",
                _ => null
            },
            Passed = name is not ("DifferentPart" or "MatchingReducer"),
            WorldFactCount = name switch { "MatchingReducer" or "SkippedRouteFailure" => 0, "RoutedReaction" => 3, "Override" => 2, "EmptyText" or "OutsideBound" or "UnauthorizedRouteFailure" or "ValidationRouteFailure" => 0, "HistoryWithoutProducer" or "CommandParts" => 2, "AnyOrderAssignment" => 3, _ => 1 },
            Routes = ExpectedRoutes(key, name)
        }).OrderBy(expectation => expectation.Specification.ToString(), StringComparer.Ordinal)];
    }

    static SemanticRejectionCategory? RejectionCategory(string name) => name switch
    {
        "EmptyText" or "OutsideBound" or "SkippedRouteFailure" => SemanticRejectionCategory.Contract,
        "UnauthorizedRouteFailure" => SemanticRejectionCategory.Unauthorized,
        "ValidationRouteFailure" => SemanticRejectionCategory.Validation,
        _ => null
    };

    static ImmutableArray<string> ExpectedRoutes(string key, string name)
    {
        if (RejectionCategory(name) is not null || name == "MatchingReducer") return [];
        if (key == "production-routes")
        {
            return name == "NoDefault" ? ["{\"sourceKind\":\"Account\",\"streamKind\":\"All\"}"] :
                ["{\"sourceKind\":\"Account\",\"streamKind\":\"Notes\",\"streamId\":\"a|b%\"}", $"{{\"sourceKind\":\"Account\",\"streamKind\":\"Periods\",\"streamId\":\"{Uuid}|a%7Cb%25\"}}"];
        }
        if (key == "observer-filters") return name == "RoutedReaction" ? ["{\"sourceKind\":\"Account\",\"streamKind\":\"Notes\",\"streamId\":\"period\"}", "null"] : ["null"];
        var route = ExpectedRoute(key, name);

        return name == "AnyOrderAssignment" ? [route, route, "null"] : [route];
    }

    static string ExpectedRoute(string key, string name)
    {
        if (name == "UnroutedFact") return "null";
        var source = key == "scalar" ? "StoredAccount" : "Account";
        var stream = name switch { "TextInput" or "HistoryWithoutProducer" => "Notes", "UUIDInput" => "UUIDs", "UnkeyedInput" or "AnyOrderAssignment" => "All", _ => key == "scalar" ? "StoredLedger" : "Ledger" };
        var id = name switch
        {
            "TextInput" => "é|%",
            "UUIDInput" => Uuid,
            "HistoryWithoutProducer" => "p-1:2026-10",
            "CommandParts" or "CommandOnlyParts" => $"{Uuid}|a%7Cb%25",
            "AppendParts" or "CommandOnlyOpaqueEscape" => $"{Uuid}|%257C",
            "DifferentPart" => $"{Uuid}|one",
            "UnkeyedInput" or "AnyOrderAssignment" => null,
            _ => (name.StartsWith("Negative", StringComparison.Ordinal) ? "-" : string.Empty) + (name.Contains("Adjacent", StringComparison.Ordinal) ? "9007199254740990" : "9007199254740991")
        };

        return id is null
            ? JsonSerializer.Serialize(new { sourceKind = source, streamKind = stream })
            : JsonSerializer.Serialize(new { sourceKind = source, streamKind = stream, streamId = id });
    }

    static ImmutableArray<byte> Resource(string name)
    {
        // The SDK replaces hyphens in resource directory names with underscores.
        var resourceName = name.Replace("composite-commands.", "composite_commands.", StringComparison.Ordinal)
            .Replace("production-routes.", "production_routes.", StringComparison.Ordinal)
            .Replace("observer-filters.", "observer_filters.", StringComparison.Ordinal);
        using var stream = typeof(EventRoutesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{resourceName}") ??
            throw new InvalidSemanticContract($"Event-routes corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
