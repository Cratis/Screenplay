// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed protected command read vectors and the identical-source legacy refusal control.
/// </summary>
public static class ProtectedCommandReadsCorpus
{
    const string ApplicationName = "ProtectedCommandReads";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.ProtectedCommandReads.v25";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated protected sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms => Forms(true);

    /// <summary>
    /// Gets the same sources with only the explicit preamble removed.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> LegacySourceForms => Forms(false);

    /// <summary>
    /// Gets the pinned identity catalog shared by the source forms.
    /// </summary>
    public static ImmutableArray<byte> IdentityCatalogBytes => Resource("identity.catalog-v1.json");

    /// <summary>
    /// Gets the pinned legacy binding diagnostics without path-dependent source coordinates.
    /// </summary>
    public static ImmutableArray<byte> LegacyDiagnosticsBytes => Resource("expected.legacy-diagnostics.json");

    /// <summary>
    /// Gets the ESM v25 protected command reads vector.
    /// </summary>
    public static CanonicalCorpusVector V25 => new()
    {
        Name = "protected-command-reads/v25",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "reference",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v25.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    /// <summary>
    /// Gets the legacy rejection vectors; no legacy executable model is fabricated.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusRejectionVector> Legacy
    {
        get
        {
            using var json = JsonDocument.Parse(LegacyDiagnosticsBytes.ToArray());
            var diagnostics = json.RootElement.EnumerateArray().Select(diagnostic => new CanonicalCorpusDiagnosticExpectation
            {
                Code = diagnostic.GetProperty("code").GetString()!,
                Message = diagnostic.GetProperty("message").GetString()!
            }).ToImmutableArray();
            return [.. LegacySourceForms.Select(form => new CanonicalCorpusRejectionVector
            {
                Name = $"protected-command-reads/legacy/{form.Name}",
                ApplicationName = ApplicationName,
                ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
                SourceForm = form,
                Diagnostics = diagnostics
            })];
        }
    }

    static ImmutableArray<CanonicalCorpusSourceForm> Forms(bool protectedReads)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName));
        var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
        var source = Encoding.UTF8.GetString(Resource("source.declarations.play").AsSpan());
        if (!protectedReads) source = source.Replace("command reads protected\n", string.Empty, StringComparison.Ordinal);
        var declarations = Document("declarations", "declarations.play", [.. Encoding.UTF8.GetBytes(source)]);
        var module = Document("module", "Storage/module.play", Resource("source.module.play"));
        var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = bytes };
        return
        [
            new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = bytes },
            folder,
            folder with { Name = "reordered", Documents = [module, declarations] },
            folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
        ];
    }

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create(ApplicationName);
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Storage", "Decisions", "Use");
        (string Name, string? Error, int Count)[] cases =
        [
            ("MissingFromEmpty", "Current 'missing' does not exist.", 0),
            ("MissingFromNonEmpty", "Current 'missing' does not exist.", 1),
            ("MissingAfterRemoval", "Current 'reference' does not exist.", 2),
            ("MissingAfterOtherRecreation", "Current 'reference' does not exist.", 3),
            ("ReadRecreatedState", null, 4),
            ("AbsenceBeforeRequirement", "Current 'missing' does not exist.", 0),
            ("RequirementAfterPresence", "Approval required.", 1),
            ("AbsenceBeforeGeneration", "Current 'missing' does not exist.", 0),
            ("GenerationAfterPresence", null, 1),
            ("ReadPathRequirementRejectsEmpty", "The stored value must not be empty.", 1),
            ("ReadPathMappingAcrossSources", null, 2),
            ("ReactionInvokesProtectedRead", null, 3),
            ("ReactionInvokesProtectedReadOfMissing", "Current 'missing' does not exist.", 1)
        ];
        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, value.Name)),
            Name = value.Name,
            Outcome = value.Error is null ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionMessage = value.Error,
            RejectionCategory = value.Error is null ? null : SemanticRejectionCategory.Validation,
            WorldFactCount = value.Count
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(ProtectedCommandReadsCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Protected command reads corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}
