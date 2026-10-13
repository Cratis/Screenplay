// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed pinned and unpinned event type name conformance vectors.
/// </summary>
public static class StoredEventNamesCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.StoredEventNames.v16";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("StoredEventNames"));
            var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Records/module.play", Resource("source.module.play"));
            var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = bytes };
            return
            [
                new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = bytes },
                folder,
                folder with { Name = "reordered", Documents = [module, declarations] },
                folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
            ];
        }
    }

    /// <summary>
    /// Gets the ESM v16 mixed stored event names vector.
    /// </summary>
    public static CanonicalCorpusVector V16 => new()
    {
        Name = "stored-event-names/v16",
        ApplicationName = "StoredEventNames",
        ApplicationIdentity = ApplicationIdentity.Create("StoredEventNames"),
        RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v16.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("StoredEventNames");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Records", "Recording", "Record");
        return
        [
            new()
            {
                Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, "MixedNames")),
                Name = "MixedNames",
                Outcome = SemanticExecutionOutcomeKind.Accepted,
                WorldFactCount = 2
            }
        ];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(StoredEventNamesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Stored event names corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
