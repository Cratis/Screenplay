// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Immutable;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents a canonical semantic model with identities replaced by structural keys and sets made order-independent.
/// </summary>
/// <param name="Root">The root node.</param>
/// <param name="FallbackKeys">The paths of set elements that had no unique key and needed the <c language="text">~n</c> fallback, sorted ordinally.</param>
internal sealed record NormalizedSemanticModel(NormalizedSemanticNode Root, ImmutableArray<string> FallbackKeys)
{
    /// <summary>
    /// Gets the compact, deterministic text of the normalised model. Two models are equivalent exactly when their texts are equal.
    /// </summary>
    public string Text => Root.Text;
}
