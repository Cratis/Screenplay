// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

/// <summary>
/// Reports selected structural completeness findings independently of compilation.
/// </summary>
public static class ModelCompleteness
{
    /// <summary>
    /// Checks a successfully compiled application; invalid models are not checked.
    /// </summary>
    /// <param name="compilation">The complete application's source compilation.</param>
    /// <param name="checks">The selected checks.</param>
    /// <returns>Opt-in warnings, or no findings when source compilation failed.</returns>
    public static ImmutableArray<Diagnostic> Check(CompilationResult<ApplicationSyntax> compilation, CompletenessChecks checks) =>
        compilation.Success ? Check(compilation.Value!, checks) : [];

    /// <summary>
    /// Checks an error-free, merged application syntax tree.
    /// </summary>
    /// <param name="application">The complete application. The caller must first verify that source compilation has no errors.</param>
    /// <param name="checks">The selected checks.</param>
    /// <returns>The selected structural warnings. These do not prove runtime completeness.</returns>
    public static ImmutableArray<Diagnostic> Check(ApplicationSyntax application, CompletenessChecks checks)
    {
        if (checks.Selected.Count == 0)
        {
            return [];
        }

        var declarations = new ConsistencyDeclarations(application, [.. ScreenplayValidator.ScopedSlices(application)]);
        var findings = ImmutableArray.CreateBuilder<Diagnostic>();
        if (checks.Selected.Contains(CompletenessCheck.DataBindings)) findings.AddRange(DataBindingCompleteness.Check(declarations));
        if (checks.Selected.Contains(CompletenessCheck.InputSurfaces)) findings.AddRange(InputSurfaceCompleteness.Check(application, declarations));
        if (checks.Selected.Contains(CompletenessCheck.FieldOrigins)) findings.AddRange(FieldOriginCompleteness.Check(declarations));

        return findings.ToImmutable();
    }
}
