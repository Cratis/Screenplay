// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the source-backed first-observation regression vector for record captures (decision 0080).
/// </summary>
public static class RecordCapturesCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.RecordCaptures.v6";

    /// <summary>
    /// Gets the source form of the record-capture vector.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RecordCaptures"));
            return
            [
                new()
                {
                    Name = "single",
                    IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(catalog)],
                    Documents =
                    [
                        new()
                        {
                            StableKey = "record-captures",
                            DisplayPath = "RecordCaptures.play",
                            Bytes = Resource("source.RecordCaptures.play")
                        }
                    ]
                }
            ];
        }
    }

    /// <summary>
    /// Gets the separately pinned ESM v6 vector; the frozen reactions vector is unchanged.
    /// </summary>
    public static CanonicalCorpusVector V6 => new()
    {
        Name = "record-captures/v6",
        ApplicationName = "RecordCaptures",
        ApplicationIdentity = ApplicationIdentity.Create("RecordCaptures"),
        RuntimeStreamId = "root",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v6.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("RecordCaptures");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Billing", "Import", "Records");
        return [.. new[] { ("FirstRecord", 5), ("ModifiedRecord", 6), ("DifferentKey", 3), ("NewChildAndNestedRecord", 6) }.Select(item => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, item.Item1)),
            Name = item.Item1,
            Outcome = SemanticExecutionOutcomeKind.Accepted,
            WorldFactCount = item.Item2
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(RecordCapturesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Record-capture corpus resource '{name}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}
