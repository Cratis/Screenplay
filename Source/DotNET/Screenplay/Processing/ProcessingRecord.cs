// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Processing;

/// <summary>
/// A declaration-derived controller processing inventory, not a legal or runtime protection verdict.
/// </summary>
/// <param name="ControllerName">The supplied controller name.</param>
/// <param name="ControllerContact">The supplied contact details.</param>
/// <param name="Rows">One row for each declared purpose.</param>
public sealed record ProcessingRecord(string? ControllerName, string? ControllerContact, IReadOnlyList<ProcessingRecordRow> Rows)
{
    /// <summary>
    /// Gets the report notice.
    /// </summary>
    public string Notice => "Generated from declarations in this model. Not legal advice.";

    /// <summary>
    /// Gets the limits of the derived inventory.
    /// </summary>
    public string Coverage => "Declared concepts reachable through known value shapes only; imports, opaque code, runtime protection and processing scale are not inferred.";

    /// <summary>
    /// Derives the inventory from an error-free merged application.
    /// </summary>
    /// <param name="application">The caller-validated application.</param>
    /// <param name="controllerName">The controller name, without inference from model names.</param>
    /// <param name="controllerContact">The controller contact details.</param>
    /// <returns>The declared processing inventory.</returns>
    public static ProcessingRecord Create(ApplicationSyntax application, string? controllerName = null, string? controllerContact = null)
    {
        var addresses = new Dictionary<SliceSyntax, string>(ReferenceEqualityComparer.Instance);
        var scopes = ScreenplayValidator.ScopedSlices(application).ToArray();
        foreach (var entry in scopes) addresses.Add(entry.Slice, string.Join('.', entry.Scope.Segments));
        var conceptsIn = PurposeCoverage.ConceptResolver(application, scopes);
        var coverage = PurposeCoverage.Slices(application).Select(entry => new CoveredSlice(entry.Slice, entry.Purposes, [.. conceptsIn(entry.Slice)])).ToArray();
        var rows = application.Purposes.Select(purpose => Row(purpose, [.. coverage.Where(entry => entry.Purposes.Any(reference => reference.Name == purpose.Name))], addresses)).ToArray();

        return new(controllerName, controllerContact, rows);
    }

    static ProcessingRecordRow Row(PurposeSyntax purpose, CoveredSlice[] covered, Dictionary<SliceSyntax, string> addresses)
    {
        var concepts = covered.SelectMany(entry => entry.Concepts).DistinctBy(concept => concept.Name).OrderBy(concept => concept.Name, StringComparer.Ordinal).ToArray();
        var personal = concepts.Where(concept => concept.Attributes.Any(attribute => attribute.Name == ConceptAttributeSyntax.Pii)).ToArray();
        var categories = personal.SelectMany(concept => concept.Attributes).Where(attribute => attribute.Name == ConceptAttributeSyntax.Pii && attribute.SpecialCategory is not null).Select(attribute => attribute.SpecialCategory!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var criminal = personal.Any(concept => concept.Attributes.Any(attribute => attribute.Criminal));
        var findings = purpose.ErasureException is not null && personal.Length > 0
            ? new[] { $"Declared erasure exception '{purpose.ErasureException}' conflicts with per-subject crypto-shredding: deleting the subject key also destroys data retained for '{purpose.Name}' ({string.Join(", ", personal.Select(concept => concept.Name))}). Decide how retained processing is protected separately." }
            : [];

        return new(
            purpose.Name,
            purpose.Description,
            purpose.Basis,
            purpose.BasisReference,
            purpose.Interest,
            purpose.Condition,
            purpose.ConditionReference,
            purpose.Authorization,
            [.. purpose.Subjects],
            [.. purpose.Recipients],
            [.. purpose.Transfers.Select(transfer => new ProcessingTransfer(transfer.Destination, transfer.Safeguard))],
            purpose.Retention,
            purpose.ErasureException,
            [.. personal.Select(concept => concept.Name)],
            categories,
            criminal,
            [.. concepts.SelectMany(Security).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            categories.Length > 0 || criminal ? "Review whether a DPIA is required: Art. 35(3)(b) covers large-scale processing of special-category or criminal-offence data. Processing scale is not declared by this model." : null,
            findings,
            [.. covered.Select(entry => addresses[entry.Slice]).Order(StringComparer.Ordinal)]);
    }

    static IEnumerable<string> Security(ConceptSyntax concept)
    {
        if (concept.Attributes.Any(attribute => attribute.Name == ConceptAttributeSyntax.Pii)) return ["Declared [PII]: per-subject encryption and crypto-shredding"];

        return concept.Attributes.Where(attribute => attribute.Name == ConceptAttributeSyntax.Sensitive)
            .Select(attribute => $"Declared [Encrypted] scope {attribute.Scope ?? "subject"} and [NotAudited]");
    }

    sealed record CoveredSlice(SliceSyntax Slice, IReadOnlyList<PurposeReferenceSyntax> Purposes, ConceptSyntax[] Concepts);
}
