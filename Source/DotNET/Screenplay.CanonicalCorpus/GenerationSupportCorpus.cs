// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed ESM v21 generation and checksum conformance vectors.
/// </summary>
public static class GenerationSupportCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.GenerationSupport.v21";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GenerationSupport"));
            var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Shop/module.play", Resource("source.module.play"));
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
    /// Gets the v21 vector, including a supplied-value checksum execution independent of generation.
    /// </summary>
    public static CanonicalCorpusVector V21 => new()
    {
        Name = "generation-support/v21",
        ApplicationName = "GenerationSupport",
        ApplicationIdentity = ApplicationIdentity.Create("GenerationSupport"),
        RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        SourceForms = SourceForms,
        SpecificationExpectations =
        [
            new()
            {
                Specification = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GenerationSupport")).ResolveSemantic(
                    SemanticAddress.ForSpecification(SemanticAddress.ForSlice(ApplicationIdentity.Create("GenerationSupport"), "Shop", "References", "Record"), "ValidStoredChecksum")),
                Name = "ValidStoredChecksum", Outcome = SemanticExecutionOutcomeKind.Accepted, WorldFactCount = 1
            }
        ],
        EsmBytes = Resource("expected.esm-v21.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(GenerationSupportCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Generation support corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
