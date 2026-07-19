// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Defines a formatter that renders <see cref="Diagnostic">diagnostics</see> for human consumption.
/// </summary>
public interface IDiagnosticFormatter
{
    /// <summary>
    /// Formats a diagnostic with its source context.
    /// </summary>
    /// <param name="file">The display path of the file the diagnostic belongs to.</param>
    /// <param name="diagnostic">The <see cref="Diagnostic"/> to format.</param>
    /// <param name="source">The source text the diagnostic refers to.</param>
    /// <param name="useColors">Whether to include ANSI colors in the output.</param>
    /// <returns>The formatted diagnostic.</returns>
    string Format(string file, Diagnostic diagnostic, string source, bool useColors);
}
