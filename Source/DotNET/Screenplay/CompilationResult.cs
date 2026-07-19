// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay;

/// <summary>
/// Represents the outcome of a compilation.
/// </summary>
/// <typeparam name="TResult">The type of value the compilation produces.</typeparam>
/// <param name="Value">The compiled value, or <c>default</c> when compilation failed.</param>
/// <param name="Diagnostics">The <see cref="Diagnostic">diagnostics</see> produced during compilation.</param>
public record CompilationResult<TResult>(TResult? Value, IEnumerable<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether the compilation succeeded without errors.
    /// </summary>
    public bool Success => Value is not null && !Diagnostics.Any(_ => _.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Creates a failed <see cref="CompilationResult{TResult}"/> from a set of diagnostics.
    /// </summary>
    /// <param name="diagnostics">The <see cref="Diagnostic">diagnostics</see> explaining the failure.</param>
    /// <returns>A failed <see cref="CompilationResult{TResult}"/>.</returns>
    public static CompilationResult<TResult> Failed(IEnumerable<Diagnostic> diagnostics) => new(default, diagnostics);
}
