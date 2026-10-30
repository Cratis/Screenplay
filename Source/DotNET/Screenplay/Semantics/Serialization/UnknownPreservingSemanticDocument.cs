// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Retains an admitted document's complete wire identity alongside its validated known projection.
/// </summary>
public sealed class UnknownPreservingSemanticDocument
{
    readonly byte[] _bytes;

    internal UnknownPreservingSemanticDocument(byte[] bytes, (LanguageVersion Language, SemanticVersion Semantic) wireVersions, SemanticRevision revision, ExecutableSemanticModel model, ImmutableArray<UnknownEsmMember> unknowns)
    {
        _bytes = bytes;
        LanguageVersion = wireVersions.Language;
        SemanticVersion = wireVersions.Semantic;
        Revision = revision;
        Model = model;
        Unknowns = unknowns;
    }

    /// <summary>Gets the original wire language version used for admission.</summary>
    public LanguageVersion LanguageVersion { get; }

    /// <summary>Gets the original wire semantic version used for admission.</summary>
    public SemanticVersion SemanticVersion { get; }

    /// <summary>Gets the revision verified over the complete canonical document, including unknowns.</summary>
    public SemanticRevision Revision { get; }

    /// <summary>
    /// Gets the strictly validated known projection at the major's highest registered pair.
    /// Its versions and revision are projection identity, not the wire document's admission identity.
    /// No known declaration or dangling reference is pruned to manufacture validity.
    /// </summary>
    public ExecutableSemanticModel Model { get; }

    /// <summary>Gets the owner-addressable opaque extension boundaries.</summary>
    public ImmutableArray<UnknownEsmMember> Unknowns { get; }

    /// <summary>Gets the number of original canonical bytes.</summary>
    public int Length => _bytes.Length;

    /// <summary>Copies the complete original canonical bytes without exposing the owned buffer.</summary>
    /// <returns>A privately owned copy of the wire document.</returns>
    public byte[] CopyBytes() => [.. _bytes];
}
