// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Defines the severity of a <see cref="Diagnostic"/>.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational message that does not affect compilation.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Something suspicious that does not prevent compilation.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A problem that prevents successful compilation.
    /// </summary>
    Error = 2
}
