// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Describes one opaque extension boundary attributed to its nearest known semantic declaration.
/// </summary>
/// <param name="Path">The member's JSON pointer in the original document.</param>
/// <param name="Owner">The nearest known declaration identity, with application fallback.</param>
/// <param name="Category">The known structural position's category.</param>
/// <param name="Value">The privately owned canonical UTF-8 JSON value bytes.</param>
public sealed record UnknownEsmMember(string Path, SemanticId Owner, UnknownEsmCategory Category, ImmutableArray<byte> Value)
{
    /// <summary>Gets the case-sensitive member name from its JSON pointer.</summary>
    public string Name => Path[(Path.LastIndexOf('/') + 1)..];

    /// <summary>
    /// Gets whether the member lies within a known security-relevant position.
    /// This is a consumer admission signal, not a claim about the opaque value's semantics.
    /// </summary>
    public bool IsSecurityRelevant => Category != UnknownEsmCategory.Unclassified;
}
