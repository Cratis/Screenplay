// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed query argument validation and rejection conformance vectors.
/// </summary>
public static class QueryValidationCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.QueryValidation.v18";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("QueryValidation"));
            var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Billing/module.play", Resource("source.module.play"));
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
    /// Gets the ESM v18 query validation rejection vector.
    /// </summary>
    public static CanonicalCorpusVector V18 => new()
    {
        Name = "query-validation/v18",
        ApplicationName = "QueryValidation",
        ApplicationIdentity = ApplicationIdentity.Create("QueryValidation"),
        RuntimeStreamId = "invoice-a",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v18.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("QueryValidation");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Billing", "Invoices", "Lookup");
        string[] names = ["InvalidKey", "InvalidThenQuery", "InvalidFilter", "InvalidFilterBare", "ValidMissing", "OptionalOmitted"];
        return [.. names.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = name.StartsWith("Invalid", StringComparison.Ordinal) ? SemanticExecutionOutcomeKind.Rejected : SemanticExecutionOutcomeKind.Accepted,
            RejectionCategory = name.StartsWith("Invalid", StringComparison.Ordinal) ? SemanticRejectionCategory.Validation : null,
            RejectionMessage = RejectionMessage(name),
            WorldFactCount = 0
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(QueryValidationCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Query validation corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };

    static string? RejectionMessage(string name)
    {
        if (!name.StartsWith("Invalid", StringComparison.Ordinal))
        {
            return null;
        }

        return name.Contains("Filter", StringComparison.Ordinal) ? "Customer code is too short" : "Invoice number is too short";
    }
}
