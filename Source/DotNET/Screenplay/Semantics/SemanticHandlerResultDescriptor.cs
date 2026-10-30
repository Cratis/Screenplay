// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>Describes the selected-event result contract of a command handler.</summary>
/// <param name="ResultVersion">The handler result contract version.</param>
/// <param name="Entries">The possible events in declaration order.</param>
public sealed record SemanticHandlerResultDescriptor(uint ResultVersion, ImmutableArray<SemanticHandlerResultEntry> Entries);
