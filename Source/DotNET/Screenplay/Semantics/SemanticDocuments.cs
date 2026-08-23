// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents an immutable Screenplay source document.
/// </summary>
public sealed record SemanticSourceDocument
{
    SemanticSourceDocument(DocumentId id, string stableKey, string displayPath, string text)
    {
        Id = id;
        StableKey = stableKey;
        DisplayPath = displayPath;
        Text = text;
    }

    /// <summary>Gets the stable document identity.</summary>
    public DocumentId Id { get; }

    /// <summary>Gets the stable, non-path identity key.</summary>
    public string StableKey { get; }

    /// <summary>Gets the display path, which never participates in identity.</summary>
    public string DisplayPath { get; }

    /// <summary>Gets the original source text.</summary>
    public string Text { get; }

    /// <summary>
    /// Creates a validated source document.
    /// </summary>
    /// <param name="id">The document identity.</param>
    /// <param name="stableKey">The stable, non-path identity key.</param>
    /// <param name="displayPath">The display path.</param>
    /// <param name="text">The original source text.</param>
    /// <returns>The immutable source document.</returns>
    /// <exception cref="InvalidSemanticContract">The identity or text fields are invalid.</exception>
    public static SemanticSourceDocument Create(DocumentId id, string stableKey, string displayPath, string text)
    {
        if (!id.IsSet || string.IsNullOrEmpty(stableKey) || string.IsNullOrEmpty(displayPath) || text is null)
        {
            throw new InvalidSemanticContract("A semantic source document is malformed.");
        }

        return new(id, stableKey.Normalize(NormalizationForm.FormC), displayPath, text);
    }
}

/// <summary>
/// Represents the source range and identity origin of a semantic artifact.
/// </summary>
/// <param name="SemanticId">The semantic identity.</param>
/// <param name="Span">The original source span.</param>
/// <param name="Origin">The identity origin, which is not part of semantic revision.</param>
public sealed record SemanticSourceMapEntry(
    SemanticId SemanticId,
    SemanticSourceSpan Span,
    SemanticIdentityOrigin Origin);

/// <summary>
/// Represents the immutable source-to-semantic map for a compilation.
/// </summary>
public sealed class SemanticSourceMap
{
    SemanticSourceMap(ImmutableArray<SemanticSourceMapEntry> entries) => Entries = entries;

    /// <summary>Gets an empty source map.</summary>
    public static SemanticSourceMap Empty { get; } = Create([], []);

    /// <summary>Gets source map entries ordered by semantic identity.</summary>
    public ImmutableArray<SemanticSourceMapEntry> Entries { get; }

    /// <summary>
    /// Creates and validates a source map.
    /// </summary>
    /// <param name="entries">The source map entries.</param>
    /// <param name="documents">The source documents the spans can reference.</param>
    /// <returns>The validated source map.</returns>
    /// <exception cref="InvalidSemanticContract">An entry is default, duplicated or references an unknown document.</exception>
    public static SemanticSourceMap Create(
        ImmutableArray<SemanticSourceMapEntry> entries,
        ImmutableArray<SemanticSourceDocument> documents)
    {
        if (entries.IsDefault || documents.IsDefault)
        {
            throw new InvalidSemanticContract("Source map arrays cannot be default.");
        }

        var documentsById = new Dictionary<DocumentId, SemanticSourceDocument>();
        foreach (var document in documents)
        {
            if (document?.Id.IsSet != true || !documentsById.TryAdd(document.Id, document))
            {
                throw new InvalidSemanticContract("A source map document is malformed or duplicated.");
            }
        }

        var semanticIds = new HashSet<SemanticId>();
        foreach (var entry in entries)
        {
            if (entry?.SemanticId.IsSet != true || !documentsById.TryGetValue(entry.Span.Document, out var document) ||
                entry.Span.Length > document.Text.Length || entry.Span.Start > document.Text.Length - entry.Span.Length ||
                !Enum.IsDefined(entry.Origin) || entry.Origin == SemanticIdentityOrigin.Unknown)
            {
                throw new InvalidSemanticContract("A semantic source map entry is malformed or unresolved.");
            }

            if (!semanticIds.Add(entry.SemanticId))
            {
                throw new InvalidSemanticContract($"Semantic source map identity '{entry.SemanticId}' is duplicated.");
            }
        }

        return new([.. entries.OrderBy(_ => _.SemanticId.ToString(), StringComparer.Ordinal)]);
    }
}

/// <summary>
/// Represents one immutable logical application source document set.
/// </summary>
public sealed class SemanticDocumentSet
{
    SemanticDocumentSet(ImmutableArray<SemanticSourceDocument> documents, SemanticIdentityCatalog identityCatalog)
    {
        Documents = documents;
        IdentityCatalog = identityCatalog;
    }

    /// <summary>Gets the documents in deterministic document identity order.</summary>
    public ImmutableArray<SemanticSourceDocument> Documents { get; }

    /// <summary>Gets the authoritative persisted identity assignments.</summary>
    public SemanticIdentityCatalog IdentityCatalog { get; }

    /// <summary>
    /// Creates a validated document set.
    /// </summary>
    /// <param name="documents">The source documents.</param>
    /// <param name="identityCatalog">The authoritative identity catalog.</param>
    /// <returns>The deterministic document set.</returns>
    /// <exception cref="InvalidSemanticContract">A document is duplicated or does not match its catalog resolution.</exception>
    public static SemanticDocumentSet Create(
        ImmutableArray<SemanticSourceDocument> documents,
        SemanticIdentityCatalog identityCatalog)
    {
        if (documents.IsDefault || documents.IsEmpty || identityCatalog is null)
        {
            throw new InvalidSemanticContract("A semantic document set requires a non-default, non-empty document array and a catalog.");
        }

        var ids = new HashSet<DocumentId>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var displayPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            if (document is null || !ids.Add(document.Id) || !keys.Add(document.StableKey) || !displayPaths.Add(document.DisplayPath))
            {
                throw new InvalidSemanticContract("A semantic source document identity, stable key or display path is duplicated.");
            }

            if (identityCatalog.ResolveDocument(document.StableKey) != document.Id)
            {
                throw new InvalidSemanticContract($"Document '{document.DisplayPath}' does not match its authoritative identity assignment.");
            }
        }

        return new([.. documents.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal)], identityCatalog);
    }
}
