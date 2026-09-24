// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the presentation severity of a failed validation rule or requirement.
/// Every failed rule rejects execution regardless of severity.
/// </summary>
public enum SemanticValidationSeverity
{
    /// <summary>The default error level.</summary>
    Error = 0,

    /// <summary>The informational level.</summary>
    Information = 1,

    /// <summary>The warning level.</summary>
    Warning = 2
}
