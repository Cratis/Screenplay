// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Describes persisted semantic or event-contract identity continuity.
/// </summary>
/// <param name="Declaration">The declaration whose identity changes.</param>
/// <param name="Change">The identity change.</param>
/// <param name="EventContractId">The contract identity for an event-contract assignment; otherwise null.</param>
public sealed record IdentityChange(
    ComparedDeclaration Declaration,
    IdentityChangeKind Change,
    EventContractId? EventContractId);
