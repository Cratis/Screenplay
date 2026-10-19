// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the vector for <c>given</c> events without an event source (decision 0101).
/// </summary>
/// <remarks>
/// A sourceless given belongs to the event source the specification acts on: constraints check it against the
/// action's own event source, and a projection that keys on the event source identity is refused rather than given
/// an invented one.
/// </remarks>
public static class SourcelessGivensCorpus
{
    const string ApplicationName = "SourcelessGivens";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.SourcelessGivens";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName))).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Banking/module.play", Resource("source.module.play"));
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
    /// Gets the sourceless given vector, an ESM v2 model.
    /// </summary>
    public static CanonicalCorpusVector V2 => new()
    {
        Name = "sourceless-givens",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "request",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create(ApplicationName);
        var catalog = SemanticIdentityCatalog.Empty(application);
        (string Slice, string Name, SemanticExecutionOutcomeKind Outcome, string? Error)[] cases =
        [
            ("Open", "RejectingASecondOpening", SemanticExecutionOutcomeKind.Rejected, "The account is already open."),
            ("Open", "OpeningBesideAnotherAccount", SemanticExecutionOutcomeKind.Accepted, null),
            ("Alias", "ReclaimingItsOwnAlias", SemanticExecutionOutcomeKind.Accepted, null),
            ("Alias", "ClaimingAnotherAccountsAlias", SemanticExecutionOutcomeKind.Rejected, "The alias is taken."),
            ("Directory", "ListingAnAliasWithoutAnAction", SemanticExecutionOutcomeKind.Accepted, null),
            ("Name", "RefusingToProjectAGivenBeforeTheCommand", SemanticExecutionOutcomeKind.Unsupported, null),
            ("Profile", "RefusingToInventAnIdentity", SemanticExecutionOutcomeKind.Unsupported, null)
        ];

        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "Banking", "Accounts", value.Slice), value.Name)),
            Name = value.Name,
            Outcome = value.Outcome,
            Passed = value.Outcome != SemanticExecutionOutcomeKind.Unsupported,
            RejectionMessage = value.Error,
            RejectionCategory = value.Error is null ? null : SemanticRejectionCategory.Constraint,
            UnsupportedCapability = value.Outcome == SemanticExecutionOutcomeKind.Unsupported ? SemanticExecutionCapability.Projection : null
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(SourcelessGivensCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Sourceless givens corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
