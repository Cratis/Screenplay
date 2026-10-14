// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// The exception that is thrown when the product walk reaches too many distinct DFA states.
/// </summary>
public sealed class RegexStateBudgetExceeded() : Exception("budget exceeded");
