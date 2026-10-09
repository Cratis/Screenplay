// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines which way a Translate slice crosses the application's public event boundary.
/// </summary>
public enum TranslationDirection
{
    /// <summary>
    /// Translates outside occurrences into local facts, including existing captures.
    /// </summary>
    Inbound = 0,

    /// <summary>
    /// Translates private local facts into a public contract.
    /// </summary>
    Outbound = 1
}
