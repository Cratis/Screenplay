// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the fallback outcomes for a guarded screen action. Values are stable and new outcomes are appended.
/// </summary>
public enum ScreenActionOtherwiseOutcome
{
    /// <summary>
    /// An outcome the compiler does not recognize.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Hide the action when no alternative matches.
    /// </summary>
    Hidden = 0,

    /// <summary>
    /// Execute the fallback command when no alternative matches.
    /// </summary>
    Execute = 1
}
