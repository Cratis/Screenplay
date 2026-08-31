// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines what a <see cref="SemanticSourceMapEntry"/> captures for its semantic identity.
/// </summary>
public enum SemanticSourceMapRole
{
    /// <summary>
    /// An unknown role. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The declaration site of the semantic identity itself.
    /// </summary>
    Declaration = 0,

    /// <summary>
    /// The single-line quoted description body owned by the semantic identity, patchable in place.
    /// </summary>
    Description = 1
}

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
        if (!id.IsSet || text is null)
        {
            throw new InvalidSemanticContract("A semantic source document is malformed.");
        }

        var normalizedKey = SemanticDocumentText.NormalizeStableKey(stableKey);
        var normalizedPath = SemanticDocumentText.NormalizeDisplayPath(displayPath);
        SemanticDocumentText.RequireWellFormedUnicode(text, "source text");

        return new(id, normalizedKey, normalizedPath, text);
    }
}

/// <summary>
/// Represents the source range and identity origin of a semantic artifact.
/// </summary>
/// <param name="SemanticId">The semantic identity.</param>
/// <param name="Span">The original source span.</param>
/// <param name="Origin">The effective semantic assignment origin, or event-contract assignment origin for an event declaration; not part of semantic revision.</param>
public sealed record SemanticSourceMapEntry(
    SemanticId SemanticId,
    SemanticSourceSpan Span,
    SemanticIdentityOrigin Origin)
{
    /// <summary>
    /// Gets the role this entry captures for <see cref="SemanticId"/>.
    /// </summary>
    /// <remarks>
    /// An <c>init</c> property rather than a parameter of the primary constructor, deliberately. A trailing
    /// parameter on a positional record is source compatible and <em>binary</em> breaking: it replaces the
    /// constructor and <c>Deconstruct</c> in the compiled signature, so a package built against the previous
    /// version fails at run time with a missing method and no compiler error anywhere. Adding capability as
    /// an init property is neither, and is how this record should grow from here.
    /// </remarks>
    public SemanticSourceMapRole Role { get; init; } = SemanticSourceMapRole.Declaration;
}

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

        var mappedSpans = new HashSet<(SemanticId SemanticId, SemanticSourceSpan Span, SemanticSourceMapRole Role)>();
        foreach (var entry in entries)
        {
            if (entry?.SemanticId.IsSet != true || !documentsById.TryGetValue(entry.Span.Document, out var document) ||
                !Enum.IsDefined(entry.Origin) || entry.Origin == SemanticIdentityOrigin.Unknown ||
                !Enum.IsDefined(entry.Role) || entry.Role == SemanticSourceMapRole.Unknown)
            {
                throw new InvalidSemanticContract("A semantic source map entry is malformed or unresolved.");
            }

            entry.Span.ValidateAgainst(document.Text);
            if (!mappedSpans.Add((entry.SemanticId, entry.Span, entry.Role)))
            {
                throw new InvalidSemanticContract($"Semantic source map identity '{entry.SemanticId}' contains a duplicate source span.");
            }
        }

        return new(
        [
            .. entries
                .OrderBy(_ => _.SemanticId.ToString(), StringComparer.Ordinal)
                .ThenBy(_ => _.Span.Document.ToString(), StringComparer.Ordinal)
                .ThenBy(_ => _.Span.Start)
                .ThenBy(_ => _.Span.Length)
                .ThenBy(_ => _.Role)
        ]);
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
        var displayPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

static class SemanticDocumentText
{
    internal static string NormalizeStableKey(string stableKey) => NormalizeStableKey(stableKey, "stable document key");

    internal static string NormalizeStableKey(string stableKey, string description)
    {
        var normalized = NormalizeRequiredUnicode(stableKey, description);
        if (string.Equals(normalized, ".", StringComparison.Ordinal) || string.Equals(normalized, "..", StringComparison.Ordinal) ||
            normalized.Contains('/') || normalized.Contains('\\') || IsDrivePath(normalized))
        {
            throw new InvalidSemanticContract($"A {description} cannot contain path semantics.");
        }

        RequireNoControlCharacters(normalized, description);
        return normalized;
    }

    internal static string NormalizeDisplayPath(string displayPath)
    {
        var normalized = NormalizeRequiredUnicode(displayPath, "source document display path").Replace('\\', '/');
        RequireNoControlCharacters(normalized, "source document display path");
        if (normalized[0] == '/' || IsDrivePath(normalized))
        {
            throw new InvalidSemanticContract("A source document display path must be portable and relative.");
        }

        var segments = normalized.Split('/');
        if (segments.Any(_ => string.IsNullOrEmpty(_) || string.Equals(_, ".", StringComparison.Ordinal) || string.Equals(_, "..", StringComparison.Ordinal)))
        {
            throw new InvalidSemanticContract("A source document display path cannot contain empty, current-directory, or traversal segments.");
        }

        return normalized;
    }

    internal static void RequireWellFormedUnicode(string value, string description)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new InvalidSemanticContract($"The {description} contains malformed Unicode.");
                }

                index++;
            }
            else if (char.IsLowSurrogate(value[index]))
            {
                throw new InvalidSemanticContract($"The {description} contains malformed Unicode.");
            }
        }
    }

    internal static string NormalizeRequiredUnicode(string value, string description)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidSemanticContract($"The {description} cannot be empty.");
        }

        RequireWellFormedUnicode(value, description);
        return value.Normalize(NormalizationForm.FormC);
    }

    static void RequireNoControlCharacters(string value, string description)
    {
        if (value.Any(char.IsControl))
        {
            throw new InvalidSemanticContract($"The {description} cannot contain control characters.");
        }
    }

    static bool IsDrivePath(string value) => value.Length >= 2 && char.IsAsciiLetter(value[0]) && value[1] == ':';
}
