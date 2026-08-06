// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

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
    /// Parses Screenplay source text into its syntax tree without resolving cross references.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="path">The path to attribute every <see cref="Diagnostics.SourceLocation"/> in the tree to.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the <see cref="ApplicationSyntax"/> and any syntax diagnostics.</returns>
    /// <remarks>
    /// <see cref="Compile(string)"/> is this plus cross reference resolution - it reports every event, policy,
    /// concept and type a document names but does not declare. That check belongs to the whole application, so
    /// a document that is one file of several has to be parsed first and resolved once the rest is in hand.
    /// Compiling a folder does exactly that; reach for this directly only when you are assembling an
    /// application from documents yourself.
    /// </remarks>
    CompilationResult<ApplicationSyntax> Parse(string source, string? path = null);

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

    /// <summary>
    /// Compiles a standalone specification document - source rooted at a <c>specification</c> declaration.
    /// </summary>
    /// <param name="source">The source text to compile.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the <see cref="SpecificationSyntax"/> and any diagnostics.</returns>
    CompilationResult<SpecificationSyntax> CompileSpecification(string source);

    /// <summary>
    /// Compiles a standalone specification document and drives a visitor over the resulting syntax tree.
    /// </summary>
    /// <typeparam name="TSpecification">The type the visitor produces.</typeparam>
    /// <param name="source">The source text to compile.</param>
    /// <param name="visitor">The <see cref="ISpecificationSyntaxVisitor{TSpecification}"/> to drive.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the visitor result and any diagnostics.</returns>
    CompilationResult<TSpecification> CompileSpecification<TSpecification>(string source, ISpecificationSyntaxVisitor<TSpecification> visitor);

    /// <summary>
    /// Compiles a standalone capture document - source rooted at a <c>capture</c> declaration.
    /// </summary>
    /// <param name="source">The source text to compile.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the <see cref="CaptureSyntax"/> and any diagnostics.</returns>
    CompilationResult<CaptureSyntax> CompileCapture(string source);

    /// <summary>
    /// Compiles a standalone capture document and drives a visitor over the resulting syntax tree.
    /// </summary>
    /// <typeparam name="TCapture">The type the visitor produces.</typeparam>
    /// <param name="source">The source text to compile.</param>
    /// <param name="visitor">The <see cref="ICaptureSyntaxVisitor{TCapture}"/> to drive.</param>
    /// <returns>A <see cref="CompilationResult{TResult}"/> holding the visitor result and any diagnostics.</returns>
    CompilationResult<TCapture> CompileCapture<TCapture>(string source, ICaptureSyntaxVisitor<TCapture> visitor);
}
