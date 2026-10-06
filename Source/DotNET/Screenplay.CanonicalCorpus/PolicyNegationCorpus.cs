// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the byte-preserving policy-negation extension's source-backed v7 vector.
/// </summary>
public static class PolicyNegationCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.PolicyNegation";

    /// <summary>
    /// Gets equivalent single, split, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("PolicyNegation"));
            var catalogBytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var policies = Document("policies", "policies.play", Resource("source.policies.play"));
            var module = Document("module", "Portal/module.play", Resource("source.module.play"));
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
    /// Gets the canonical bytes and revision that pin policy negation independently of the original v7 vector.
    /// </summary>
    public static CanonicalCorpusVector V7 => new()
    {
        Name = "policy-negation/v7",
        ApplicationName = "PolicyNegation",
        ApplicationIdentity = ApplicationIdentity.Create("PolicyNegation"),
        RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v7.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("PolicyNegation");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Portal", "Reports", "FileReport");
        return [.. new[] { "PersonAllowed", "ServiceRoleDenied", "ServiceClaimDenied", "UnauthenticatedDenied" }.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = name == "PersonAllowed" ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionCategory = name == "PersonAllowed" ? null : SemanticRejectionCategory.Unauthorized,
            WorldFactCount = name == "PersonAllowed" ? 1 : 0
        }).OrderBy(expectation => expectation.Specification.ToString(), StringComparer.Ordinal)];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(PolicyNegationCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Policy-negation corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
