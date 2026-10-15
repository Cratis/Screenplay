// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides additive ESM v21 clock, checksum, numeric and opaque-generation fixture outcomes.
/// </summary>
public static class GenerationFixturesCorpus
{
    const string ApplicationName = "GenerationFixtures";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.GenerationFixtures.v21";

    /// <summary>
    /// Gets equivalent source and relocated forms without changing prior corpus bytes.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName));
            var document = new CanonicalCorpusDocument { StableKey = "application", DisplayPath = "application.play", Bytes = Resource("source.application.play") };
            var form = new CanonicalCorpusSourceForm { Name = "single", Documents = [document], IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(catalog)] };

            return [form, form with { Name = "relocated", Documents = [document with { DisplayPath = "Archive/application.play" }] }];
        }
    }

    /// <summary>
    /// Gets the frozen fixture outcomes and canonical executable model.
    /// </summary>
    public static CanonicalCorpusVector V21 => new()
    {
        Name = "generation-fixtures/v21",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        SourceForms = SourceForms,
        EsmBytes = Resource("expected.esm-v21.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim()),
        SpecificationExpectations = [.. ImmutableArray.Create(
            Expect("UtcLeapDay"),
            Expect("WrongUtcDate", true),
            Expect("RepeatedDateMismatch", true),
            Expect("WidthOnlyWithoutClock"),
            Expect("ChecksumShapeOnly"),
            Expect("AuthoredChecksumArithmetic", true),
            Expect("NumericFixture"),
            Expect("GeneratorBodyNotExecuted"),
            Expect("FreshGeneratedSource", worldFactCount: 2),
            Expect("GuardedSourceReuse", true, 1, SemanticRejectionCategory.Concurrency))
            .OrderBy(expectation => expectation.Specification.ToString(), StringComparer.Ordinal)]
    };

    static CanonicalCorpusSpecificationExpectation Expect(string name, bool rejected = false, int worldFactCount = 0, SemanticRejectionCategory category = SemanticRejectionCategory.Contract) => new()
    {
        Specification = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName)).ResolveSemantic(
            SemanticAddress.ForSpecification(SemanticAddress.ForSlice(ApplicationIdentity.Create(ApplicationName), "Shop", "References", "Issue"), name)),
        Name = name,
        Outcome = rejected ? SemanticExecutionOutcomeKind.Rejected : SemanticExecutionOutcomeKind.Accepted,
        RejectionCategory = rejected ? category : null,
        WorldFactCount = worldFactCount
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(GenerationFixturesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Generation fixtures corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }
}
