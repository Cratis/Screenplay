// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// The exception that is thrown when regex construction or the product walk exceeds its budget.
/// </summary>
/// <param name="reason">The exhausted budget.</param>
public sealed class RegexStateBudgetExceeded(string reason = "budget exceeded") : Exception(reason);
