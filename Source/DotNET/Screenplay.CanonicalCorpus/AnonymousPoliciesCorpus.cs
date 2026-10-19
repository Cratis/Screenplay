// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed anonymous policy condition (decision 0103) vectors.
/// </summary>
public static class AnonymousPoliciesCorpus
{
    const string ApplicationName = "AnonymousPolicies";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.AnonymousPolicies.v31";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName))).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Catalog/module.play", Resource("source.module.play"));
            var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = catalog };

            return
            [
                new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = catalog },
                folder,
                folder with { Name = "reordered", Documents = [module, declarations] },
                folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
            ];
        }
    }

    /// <summary>
    /// Gets the ESM v31 anonymous policy condition (decision 0103) vector.
    /// </summary>
    public static CanonicalCorpusVector V31 => new()
    {
        Name = "anonymous-policies/v31",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "interest",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v31.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create(ApplicationName);
        var catalog = SemanticIdentityCatalog.Empty(application);
        (string Feature, string Slice, string Name, string? Error, int Count)[] cases =
        [
            ("Browsing", "RegisterInterest", "AVisitorWithoutAnIdentityRegistersInterest", null, 1),
            ("Browsing", "RegisterInterest", "ASignedInCustomerRegistersInterest", null, 1)
        ];

        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "Catalog", value.Feature, value.Slice), value.Name)),
            Name = value.Name,
            Outcome = value.Error is null ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionMessage = value.Error,
            RejectionCategory = value.Error is null ? null : SemanticRejectionCategory.Validation,
            WorldFactCount = value.Count
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(AnonymousPoliciesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Anonymous policies corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
