// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Represents a privately owned canonical ESM document verified by the strict reader.
/// </summary>
[SuppressMessage("Usage", "MA0182", Justification = "Internal strict-document boundary seam for #1067; production callers join with the wire admission work.")]
internal sealed class StrictSemanticDocument
{
    readonly byte[] _bytes;

    StrictSemanticDocument(byte[] bytes, ExecutableSemanticModel model)
    {
        _bytes = bytes;
        Model = model;
        LanguageVersion = model.LanguageVersion;
        SemanticVersion = model.SemanticVersion;
        Revision = model.Revision;
    }

    /// <summary>
    /// Gets the document's source language version.
    /// </summary>
    public LanguageVersion LanguageVersion { get; }

    /// <summary>
    /// Gets the document's portable execution semantic version.
    /// </summary>
    public SemanticVersion SemanticVersion { get; }

    /// <summary>
    /// Gets the original root revision verified by the strict reader.
    /// </summary>
    public SemanticRevision Revision { get; }

    /// <summary>
    /// Gets the validated executable semantic model projection.
    /// </summary>
    public ExecutableSemanticModel Model { get; }

    /// <summary>
    /// Gets the number of original canonical bytes.
    /// </summary>
    public int Length => _bytes.Length;

    /// <summary>
    /// Copies canonical UTF-8 ESM JSON and verifies the owned copy with the strict reader.
    /// </summary>
    /// <param name="json">The canonical UTF-8 JSON bytes.</param>
    /// <returns>The verified document retaining its original bytes.</returns>
    /// <exception cref="InvalidSemanticContract">The JSON is refused by the strict reader.</exception>
    public static StrictSemanticDocument Read(ReadOnlySpan<byte> json)
    {
        var owned = json.ToArray();
        var model = SemanticModelSerializer.Deserialize(owned);
        return new(owned, model);
    }

    /// <summary>
    /// Copies the original canonical bytes into a new array.
    /// </summary>
    /// <returns>A privately owned copy of the original canonical bytes.</returns>
    public byte[] CopyBytes() => [.. _bytes];
}
