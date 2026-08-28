// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines the source encoding admitted by the first workspace contract.
/// </summary>
public enum WorkspaceTextEncoding
{
    /// <summary>
    /// Strict UTF-8 without a byte-order mark.
    /// </summary>
    Utf8 = 0,

    /// <summary>
    /// Strict UTF-8 with a byte-order mark.
    /// </summary>
    Utf8WithBom = 1
}

/// <summary>
/// Represents one immutable exact-byte Screenplay workspace document.
/// </summary>
public sealed class WorkspaceDocument
{
    static readonly byte[] _utf16BigEndianBom = [0xfe, 0xff];
    static readonly byte[] _utf16LittleEndianBom = [0xff, 0xfe];
    static readonly byte[] _utf32BigEndianBom = [0x00, 0x00, 0xfe, 0xff];
    static readonly byte[] _utf7PlusBom = [0x2b, 0x2f, 0x76, 0x2b];
    static readonly byte[] _utf7SlashBom = [0x2b, 0x2f, 0x76, 0x2f];
    static readonly byte[] _utf7V8Bom = [0x2b, 0x2f, 0x76, 0x38];
    static readonly byte[] _utf7V9Bom = [0x2b, 0x2f, 0x76, 0x39];
    static readonly byte[] _utf8Bom = [0xef, 0xbb, 0xbf];
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    WorkspaceDocument(
        DocumentId id,
        string stableKey,
        PortablePlayPath path,
        WorkspaceTextEncoding encoding,
        ImmutableArray<byte> bytes,
        string text)
    {
        Id = id;
        StableKey = stableKey;
        Path = path;
        Encoding = encoding;
        Bytes = bytes;
        Text = text;
    }

    /// <summary>
    /// Gets the stable document identity.
    /// </summary>
    public DocumentId Id { get; }

    /// <summary>
    /// Gets the stable non-path document key.
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// Gets the normalized portable document path.
    /// </summary>
    public PortablePlayPath Path { get; }

    /// <summary>
    /// Gets the admitted source encoding.
    /// </summary>
    public WorkspaceTextEncoding Encoding { get; }

    /// <summary>
    /// Gets an immutable copy of the exact original source bytes.
    /// </summary>
    public ImmutableArray<byte> Bytes { get; }

    /// <summary>
    /// Gets the strictly decoded source text derived from <see cref="Bytes"/>.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Creates a document with a provisional identity derived from its stable key.
    /// </summary>
    /// <param name="stableKey">The stable non-path key.</param>
    /// <param name="path">The portable source path.</param>
    /// <param name="bytes">The exact source bytes.</param>
    /// <returns>The immutable workspace document.</returns>
    /// <exception cref="InvalidWorkspaceDocument">The key, path, or source bytes are invalid.</exception>
    public static WorkspaceDocument Create(
        string stableKey,
        PortablePlayPath path,
        ReadOnlySpan<byte> bytes)
    {
        var normalizedKey = NormalizeStableKey(stableKey);
        return CreateCore(DocumentId.Create(normalizedKey), normalizedKey, path, bytes);
    }

    /// <summary>
    /// Creates a document with a persisted identity.
    /// </summary>
    /// <param name="id">The persisted document identity.</param>
    /// <param name="stableKey">The stable non-path key.</param>
    /// <param name="path">The portable source path.</param>
    /// <param name="bytes">The exact source bytes.</param>
    /// <returns>The immutable workspace document.</returns>
    /// <exception cref="InvalidWorkspaceDocument">The identity, key, or source bytes are invalid.</exception>
    public static WorkspaceDocument Create(
        DocumentId id,
        string stableKey,
        PortablePlayPath path,
        ReadOnlySpan<byte> bytes)
    {
        if (!id.IsSet)
        {
            throw new InvalidWorkspaceDocument("A workspace document identity must be set.");
        }

        return CreateCore(id, NormalizeStableKey(stableKey), path, bytes);
    }

    static WorkspaceDocument CreateCore(
        DocumentId id,
        string normalizedKey,
        PortablePlayPath path,
        ReadOnlySpan<byte> bytes)
    {
        if (path is null)
        {
            throw new InvalidWorkspaceDocument("A workspace document path must be provided.");
        }

        var snapshot = ImmutableArray.CreateRange(bytes.ToArray());
        var snapshotBytes = snapshot.AsSpan();
        if (HasUnsupportedBom(snapshotBytes))
        {
            throw new InvalidWorkspaceDocument("Workspace document bytes use an unsupported encoding signature; version 1 admits strict UTF-8 only.");
        }

        var (encoding, offset) = HasUtf8Bom(snapshotBytes)
            ? (WorkspaceTextEncoding.Utf8WithBom, _utf8Bom.Length)
            : (WorkspaceTextEncoding.Utf8, 0);
        try
        {
            var text = _strictUtf8.GetString(snapshotBytes[offset..]);
            return new WorkspaceDocument(id, normalizedKey, path, encoding, snapshot, text);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidWorkspaceDocument(
                "Workspace document bytes must be strict UTF-8, optionally with a UTF-8 byte-order mark.",
                exception);
        }
    }

    static string NormalizeStableKey(string stableKey)
    {
        try
        {
            return SemanticDocumentText.NormalizeStableKey(stableKey);
        }
        catch (InvalidSemanticContract exception)
        {
            throw new InvalidWorkspaceDocument(exception.Message, exception);
        }
    }

    static bool HasUtf8Bom(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= _utf8Bom.Length && bytes[.._utf8Bom.Length].SequenceEqual(_utf8Bom);

    static bool HasUnsupportedBom(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith(_utf16LittleEndianBom) ||
        bytes.StartsWith(_utf16BigEndianBom) ||
        bytes.StartsWith(_utf32BigEndianBom) ||
        bytes.StartsWith(_utf7V8Bom) ||
        bytes.StartsWith(_utf7V9Bom) ||
        bytes.StartsWith(_utf7PlusBom) ||
        bytes.StartsWith(_utf7SlashBom);
}

/// <summary>
/// The exception that is thrown when a workspace document contract is invalid.
/// </summary>
/// <param name="message">The reason the document was rejected.</param>
/// <param name="innerException">The exception that caused the document to be rejected, when available.</param>
public sealed class InvalidWorkspaceDocument(string message, Exception? innerException = null)
    : Exception(message, innerException);
