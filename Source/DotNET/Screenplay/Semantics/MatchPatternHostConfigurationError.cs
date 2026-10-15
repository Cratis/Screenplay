// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The exception that is thrown when the host cannot construct an admitted linear match pattern.
/// </summary>
/// <param name="inner">The engine configuration failure.</param>
public sealed class MatchPatternHostConfigurationError(Exception inner) : Exception(
    "The host cannot construct an admitted portable match pattern. Check REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE; this is a host configuration error, not a validation verdict.", inner);
