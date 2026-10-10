// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed conformance vectors for reaction system authorization identity.
/// </summary>
public static class ReactionIdentityCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.ReactionIdentity.v10";

    static readonly string[] _specificationNames = ["DeclaredIdentityAllows", "NoIdentityDenies"];

    /// <summary>
    /// Gets equivalent single-file, folder, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ReactionIdentity"));
            var catalogBytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var policies = Document("policies", "policies.play", Resource("source.policies.play"));
            var module = Document("module", "Work/module.play", Resource("source.module.play"));
            var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [policies, module], IdentityCatalogBytes = catalogBytes };

            return
            [
                new() { Name = "single", Documents = [Document("application", "application.play", [.. policies.Bytes, .. module.Bytes])], IdentityCatalogBytes = catalogBytes },
                folder,
                folder with { Name = "reordered", Documents = [module, policies] },
                folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, policies with { DisplayPath = "Archive/policies.play" }] }
            ];
        }
    }

    /// <summary>
    /// Gets the ESM v10 vector: role-authorized invocation with identity and unauthorized invocation without it.
    /// </summary>
    public static CanonicalCorpusVector V10 => new()
    {
        Name = "reaction-identity/v10",
        ApplicationName = "ReactionIdentity",
        ApplicationIdentity = ApplicationIdentity.Create("ReactionIdentity"),
        RuntimeStreamId = "one",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v10.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("ReactionIdentity");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Work", "Completion", "CompleteWork");

        return [.. _specificationNames.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = name == "DeclaredIdentityAllows" ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionCategory = name == "DeclaredIdentityAllows" ? null : SemanticRejectionCategory.Unauthorized,
            WorldFactCount = name == "DeclaredIdentityAllows" ? 2 : 1
        }).OrderBy(expectation => expectation.Specification.ToString(), StringComparer.Ordinal)];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(ReactionIdentityCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Reaction identity corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
