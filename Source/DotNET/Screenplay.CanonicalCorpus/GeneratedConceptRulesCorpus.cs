// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed generated concept rule conformance vectors.
/// </summary>
public static class GeneratedConceptRulesCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.GeneratedConceptRules.v20";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GeneratedConceptRules"));
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
    /// Gets the ESM v20 generated concept rule vector.
    /// </summary>
    public static CanonicalCorpusVector V20 => new()
    {
        Name = "generated-concept-rules/v20",
        ApplicationName = "GeneratedConceptRules",
        ApplicationIdentity = ApplicationIdentity.Create("GeneratedConceptRules"),
        RuntimeStreamId = "cus_01arz3ndektsv4rrffq69g5fav",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v20.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("GeneratedConceptRules");
        var catalog = SemanticIdentityCatalog.Empty(application);
        CanonicalCorpusSpecificationExpectation Expect(string slice, string name, SemanticExecutionOutcomeKind outcome, int facts) => new()
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "Shop", "Customers", slice), name)),
            Name = name,
            Outcome = outcome,
            RejectionCategory = outcome == SemanticExecutionOutcomeKind.Rejected ? SemanticRejectionCategory.Contract : null,
            UnsupportedCapability = outcome == SemanticExecutionOutcomeKind.Unsupported ? SemanticExecutionCapability.Command : null,
            WorldFactCount = facts
        };

        return [Expect("Register", "ProvenCustomerId", SemanticExecutionOutcomeKind.Accepted, 1), Expect("Invite", "InvalidInviteFixture", SemanticExecutionOutcomeKind.Rejected, 0), Expect("Review", "OpaqueInviteRule", SemanticExecutionOutcomeKind.Unsupported, 0)];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(GeneratedConceptRulesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Generated concept rule corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
