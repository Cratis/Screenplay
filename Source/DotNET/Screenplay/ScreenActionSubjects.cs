// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay;

/// <summary>
/// Resolves action subjects and inputs against the complete application's declarations.
/// </summary>
public static class ScreenActionSubjects
{
    /// <summary>
    /// Creates an effective application without modifying the authored syntax tree.
    /// </summary>
    /// <param name="application">The assembled application, including imported declarations.</param>
    /// <returns>The effective syntax and subject-resolution diagnostics.</returns>
    public static CompilationResult<ApplicationSyntax> Resolve(ApplicationSyntax application) => new ActionSubjectResolver(application).Resolve();
}
