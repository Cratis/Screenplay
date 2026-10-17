// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Maps a <see cref="SemanticDecisionReadRefusal"/> to the stable keyword published as the PLAY0781 <c>reason</c> argument.
/// </summary>
public static class SemanticDecisionReadRefusalKeywords
{
    /// <summary>
    /// Gets the published keyword of a refusal reason.
    /// </summary>
    /// <param name="refusal">The <see cref="SemanticDecisionReadRefusal"/> to name.</param>
    /// <returns>The lowercase, hyphenated keyword that never changes once published.</returns>
    /// <exception cref="InvalidSemanticContract">Thrown when the refusal has no published keyword.</exception>
    public static string Keyword(this SemanticDecisionReadRefusal refusal) => refusal switch
    {
        SemanticDecisionReadRefusal.Reducer => "reducer",
        SemanticDecisionReadRefusal.Join => "join",
        SemanticDecisionReadRefusal.Children => "children",
        SemanticDecisionReadRefusal.OpenSubscription => "open-subscription",
        SemanticDecisionReadRefusal.PayloadKey => "payload-key",
        SemanticDecisionReadRefusal.ByParts => "by-parts",
        SemanticDecisionReadRefusal.NoRemoval => "no-removal",
        SemanticDecisionReadRefusal.EmptyHandledSet => "empty-handled-set",
        SemanticDecisionReadRefusal.NamedSourceOrStream => "named-source-or-stream",
        SemanticDecisionReadRefusal.Singleton => "singleton",
        SemanticDecisionReadRefusal.KeyType => "key-type",
        SemanticDecisionReadRefusal.GeneratedKey => "generated-key",
        SemanticDecisionReadRefusal.OptionalKey => "optional-key",
        SemanticDecisionReadRefusal.CollectionKey => "collection-key",
        _ => throw new InvalidSemanticContract($"Decision read refusal '{refusal}' has no published keyword.")
    };
}
