// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed equality query filter conformance vectors.
/// </summary>
public static class QueryFiltersCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.QueryFilters.v14";
    static readonly string[] _specificationNames = ["ReversedRows", "DistinctCalls"];

    /// <summary>
    /// Gets equivalent single-file, folder, reordered and relocated sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("QueryFilters"));
            var catalogBytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Items/module.play", Resource("source.module.play"));
            var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = catalogBytes };

            return
            [
                new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = catalogBytes },
                folder,
                folder with { Name = "reordered", Documents = [module, declarations] },
                folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
            ];
        }
    }

    /// <summary>
    /// Gets the ESM v14 equality filters and unordered collection comparison vector.
    /// </summary>
    public static CanonicalCorpusVector V14 => new()
    {
        Name = "query-filters/v14",
        ApplicationName = "QueryFilters",
        ApplicationIdentity = ApplicationIdentity.Create("QueryFilters"),
        RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v14.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("QueryFilters");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Items", "Browsing", "Browse");

        return [.. _specificationNames.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = SemanticExecutionOutcomeKind.Accepted,
            WorldFactCount = 0
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(QueryFiltersCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Query filters corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
