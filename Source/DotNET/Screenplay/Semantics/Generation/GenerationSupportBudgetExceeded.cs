// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// The exception that is thrown when generation support construction exceeds a hard budget.
/// </summary>
/// <param name="budget">The budget name and exact limit.</param>
public sealed class GenerationSupportBudgetExceeded(string budget) : Exception($"Generation support {budget} exceeded.");
