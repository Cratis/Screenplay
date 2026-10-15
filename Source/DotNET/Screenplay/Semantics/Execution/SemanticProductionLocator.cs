// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Locates a rejected production without exposing its constrained values.
/// </summary>
/// <param name="ProductionIndex">The zero-based authored production index in its batch.</param>
/// <param name="EventContract">The produced event contract.</param>
/// <param name="Route">The admitted route, absent for direct reaction productions.</param>
public sealed record SemanticProductionLocator(int ProductionIndex, SemanticId EventContract, SemanticEventRoute? Route);
