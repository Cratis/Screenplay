// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed command Acme Commerce admission (decision 0098) vectors.
/// </summary>
public static class AcmeAdmissionsCorpus
{
    const string ApplicationName = "AcmeAdmissions";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.AcmeAdmissions.v29";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName))).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Shop/module.play", Resource("source.module.play"));
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
    /// Gets the ESM v29 Acme Commerce admission (decision 0098) vector.
    /// </summary>
    public static CanonicalCorpusVector V29 => new()
    {
        Name = "uuid-comparisons/v29",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "request",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v29.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create(ApplicationName);
        var catalog = SemanticIdentityCatalog.Empty(application);
        (string Feature, string Slice, string Name, string? Error, int Count)[] cases =
        [
            ("Pricing", "SetPrice", "SettingAPricePublishesIt", null, 2),
            ("Pricing", "SetPrice", "RejectingAThirdDecimal", "Money has at most two decimals.", 0),
            ("Basket", "PutInBasket", "AddingALine", null, 1),
            ("Stock", "MoveStock", "MovingStockForAnOrder", null, 1)
        ];

        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "Shop", value.Feature, value.Slice), value.Name)),
            Name = value.Name,
            Outcome = value.Error is null ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionMessage = value.Error,
            RejectionCategory = value.Error is null ? null : SemanticRejectionCategory.Validation,
            WorldFactCount = value.Count
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(AcmeAdmissionsCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Acme admissions corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
