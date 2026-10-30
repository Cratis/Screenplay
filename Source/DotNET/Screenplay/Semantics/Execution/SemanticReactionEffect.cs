// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// One modeled event or command payload in a homogeneous reaction batch. It supplies no routing or occurrence.
/// </summary>
/// <param name="Target">The event contract or command identity.</param>
/// <param name="Payload">The typed composite payload keyed by declared property IDs.</param>
public sealed record SemanticReactionEffect(SemanticId Target, SemanticValue Payload);
