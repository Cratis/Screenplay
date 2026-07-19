// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay;

/// <summary>
/// Defines the compiler for the Screenplay language.
/// </summary>
public interface IScreenplayCompiler
{
    /// <summary>
    /// Compiles Screenplay source text into its syntax tree.
    /// </summary>
    /// <param name="source">The source text to compile.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the <see cref="ApplicationSyntax"/> and any diagnostics.</returns>
    CompilationResult<ApplicationSyntax> Compile(string source);

    /// <summary>
    /// Compiles Screenplay source text and drives a visitor over the resulting syntax tree.
    /// </summary>
    /// <typeparam name="TApplication">The type the visitor produces.</typeparam>
    /// <param name="source">The source text to compile.</param>
    /// <param name="visitor">The <see cref="IApplicationSyntaxVisitor{TApplication}"/> to drive.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the visitor result and any diagnostics.</returns>
    CompilationResult<TApplication> Compile<TApplication>(string source, IApplicationSyntaxVisitor<TApplication> visitor);

    /// <summary>
    /// Compiles a standalone projection document - source rooted at a <c>projection</c> declaration.
    /// </summary>
    /// <param name="source">The source text to compile.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the <see cref="ProjectionSyntax"/> and any diagnostics.</returns>
    CompilationResult<ProjectionSyntax> CompileProjection(string source);

    /// <summary>
    /// Compiles a standalone projection document and drives a visitor over the resulting syntax tree.
    /// </summary>
    /// <typeparam name="TProjection">The type the visitor produces.</typeparam>
    /// <param name="source">The source text to compile.</param>
    /// <param name="visitor">The <see cref="IProjectionSyntaxVisitor{TProjection}"/> to drive.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the visitor result and any diagnostics.</returns>
    CompilationResult<TProjection> CompileProjection<TProjection>(string source, IProjectionSyntaxVisitor<TProjection> visitor);
}
