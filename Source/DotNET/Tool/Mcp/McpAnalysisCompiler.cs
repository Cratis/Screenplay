// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Tool.Mcp;

// Retain original fragments, including recoverable erroneous trees, while the existing
// folder compiler performs its normal merge and validation. No parser behavior is replaced.
sealed class McpAnalysisCompiler : IScreenplayCompiler
{
    readonly ScreenplayCompiler _compiler = new();
    readonly List<CompilationResult<ApplicationSyntax>> _documents = [];

    internal IEnumerable<CompilationResult<ApplicationSyntax>> Documents => _documents;

    internal int ParsedDocumentCount => _documents.Count;

    /// <inheritdoc/>
    public CompilationResult<ApplicationSyntax> Parse(string source, string? path = null)
    {
        var result = _compiler.Parse(source, path);
        _documents.Add(result);
        return result;
    }

    /// <inheritdoc/>
    public CompilationResult<ApplicationSyntax> Compile(string source) => _compiler.Compile(source);

    /// <inheritdoc/>
    public CompilationResult<TApplication> Compile<TApplication>(string source, IApplicationSyntaxVisitor<TApplication> visitor) => _compiler.Compile(source, visitor);

    /// <inheritdoc/>
    public CompilationResult<ProjectionSyntax> CompileProjection(string source) => _compiler.CompileProjection(source);

    /// <inheritdoc/>
    public CompilationResult<TProjection> CompileProjection<TProjection>(string source, IProjectionSyntaxVisitor<TProjection> visitor) => _compiler.CompileProjection(source, visitor);

    /// <inheritdoc/>
    public CompilationResult<SpecificationSyntax> CompileSpecification(string source) => _compiler.CompileSpecification(source);

    /// <inheritdoc/>
    public CompilationResult<TSpecification> CompileSpecification<TSpecification>(string source, ISpecificationSyntaxVisitor<TSpecification> visitor) => _compiler.CompileSpecification(source, visitor);

    /// <inheritdoc/>
    public CompilationResult<CaptureSyntax> CompileCapture(string source) => _compiler.CompileCapture(source);

    /// <inheritdoc/>
    public CompilationResult<TCapture> CompileCapture<TCapture>(string source, ICaptureSyntaxVisitor<TCapture> visitor) => _compiler.CompileCapture(source, visitor);
}
