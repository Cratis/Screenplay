// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents the most a loss ledger may accept.
/// </summary>
/// <param name="MaximumEntries">The most loss entries.</param>
/// <param name="MaximumByDesign">The most entries accepted by design rather than tracked by an issue.</param>
/// <param name="MaximumExclusions">The most excluded fixtures.</param>
internal sealed record SemanticLossBudget(int MaximumEntries, int MaximumByDesign, int MaximumExclusions);
