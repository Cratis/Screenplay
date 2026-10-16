// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the reachable reasons a declared view cannot participate in protected decision reads.
/// </summary>
public enum SemanticDecisionReadRefusal
{
    /// <summary>
    /// The view is built by a reducer.
    /// </summary>
    Reducer,

    /// <summary>
    /// The projection joins, including removal joins.
    /// </summary>
    Join,

    /// <summary>
    /// The projection has children or nested scopes.
    /// </summary>
    Children,

    /// <summary>
    /// The projection declares every or all rather than a finite explicit subscription.
    /// </summary>
    OpenSubscription,

    /// <summary>
    /// An affected instance is routed by event payload instead of event-source identity.
    /// </summary>
    PayloadKey,

    /// <summary>
    /// The lookup or projection uses composite key parts.
    /// </summary>
    ByParts,

    /// <summary>
    /// No removal event is declared for the view.
    /// </summary>
    NoRemoval,

    /// <summary>
    /// No finite nonempty handled event set is declared.
    /// </summary>
    EmptyHandledSet,

    /// <summary>
    /// The projection or handled event is routed through a named source, stream or sequence.
    /// </summary>
    NamedSourceOrStream,

    /// <summary>
    /// The view has no event-source instance key.
    /// </summary>
    Singleton,

    /// <summary>
    /// The lookup key is neither text nor a canonical UUID identity.
    /// </summary>
    KeyType,

    /// <summary>
    /// The key is generated after reads must run.
    /// </summary>
    GeneratedKey,

    /// <summary>
    /// The lookup key is optional.
    /// </summary>
    OptionalKey,

    /// <summary>
    /// The lookup key is a collection.
    /// </summary>
    CollectionKey
}
