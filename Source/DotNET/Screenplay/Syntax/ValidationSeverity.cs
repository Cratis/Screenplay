// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Describes the presentation severity of a failed validation rule or requirement.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>A failed rule is an error (the default).</summary>
    Error = 0,

    /// <summary>A failed rule is informational.</summary>
    Information = 1,

    /// <summary>A failed rule is a warning.</summary>
    Warning = 2
}
