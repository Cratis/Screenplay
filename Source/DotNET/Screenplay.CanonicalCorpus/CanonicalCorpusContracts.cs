// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Represents one immutable source document in a canonical corpus form.
/// </summary>
public sealed record CanonicalCorpusDocument
{
    static readonly UTF8Encoding _strictUtf8 = new(false, true);

    /// <summary>
    /// Gets the stable non-path document key.
    /// </summary>
    public required string StableKey { get; init; }

    /// <summary>
    /// Gets the portable display path.
    /// </summary>
    public required string DisplayPath { get; init; }

    /// <summary>
    /// Gets the exact UTF-8 source bytes.
    /// </summary>
    public required ImmutableArray<byte> Bytes { get; init; }

    /// <summary>
    /// Gets the strictly decoded source text.
    /// </summary>
    public string Text => _strictUtf8.GetString(Bytes.AsSpan());
}

/// <summary>
/// Represents one physical source form of a canonical corpus.
/// </summary>
public sealed record CanonicalCorpusSourceForm
{
    /// <summary>
    /// Gets the stable source-form name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets source documents in canonical path order.
    /// </summary>
    public ImmutableArray<CanonicalCorpusDocument> Documents { get; init; } = [];

    /// <summary>
    /// Gets the canonical identity catalog for this physical source form.
    /// </summary>
    public ImmutableArray<byte> IdentityCatalogBytes { get; init; } = [];
}

/// <summary>
/// Represents one expected normalized specification outcome in a canonical corpus.
/// </summary>
public sealed record CanonicalCorpusSpecificationExpectation
{
    /// <summary>
    /// Gets the exact specification semantic identity.
    /// </summary>
    public required SemanticId Specification { get; init; }

    /// <summary>
    /// Gets the specification display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the expected normalized execution outcome.
    /// </summary>
    public required SemanticExecutionOutcomeKind Outcome { get; init; }

    /// <summary>
    /// Gets the expected rejection message, or <see langword="null"/> for an accepted scenario.
    /// </summary>
    public string? RejectionMessage { get; init; }
}

/// <summary>
/// Represents one versioned canonical semantic conformance corpus.
/// </summary>
public sealed record CanonicalCorpusVector
{
    /// <summary>
    /// Gets the corpus identity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the application display name and stable key.
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// Gets the persisted application identity.
    /// </summary>
    public required ApplicationIdentity ApplicationIdentity { get; init; }

    /// <summary>
    /// Gets the fixed runtime stream identity used by specifications.
    /// </summary>
    public required string RuntimeStreamId { get; init; }

    /// <summary>
    /// Gets every equivalent physical source form.
    /// </summary>
    public ImmutableArray<CanonicalCorpusSourceForm> SourceForms { get; init; } = [];

    /// <summary>
    /// Gets expected normalized specification outcomes in semantic identity order.
    /// </summary>
    public ImmutableArray<CanonicalCorpusSpecificationExpectation> SpecificationExpectations { get; init; } = [];

    /// <summary>
    /// Gets canonical ESM bytes.
    /// </summary>
    public ImmutableArray<byte> EsmBytes { get; init; } = [];

    /// <summary>
    /// Gets the expected semantic revision.
    /// </summary>
    public SemanticRevision SemanticRevision { get; init; }
}
