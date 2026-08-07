// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayCompiler"/>.
/// </summary>
/// <param name="languages">The <see cref="IScreenplayLanguageRegistry"/> saying what to recognize beyond the built-in constructs.</param>
public class ScreenplayCompiler(IScreenplayLanguageRegistry languages) : IScreenplayCompiler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayCompiler"/> class recognizing only what the
    /// language ships with.
    /// </summary>
    /// <remarks>
    /// Declared rather than expressed as a default argument on the primary constructor. A defaulted parameter
    /// replaces the parameterless constructor in the compiled signature, so every consumer that wrote
    /// <c>new ScreenplayCompiler()</c> against a previous version would fail to bind at run time without a
    /// single compiler error anywhere - the quietest kind of break there is. Spelling it out keeps
    /// <c>.ctor()</c> in the surface, and the registry is a pure addition.
    /// </remarks>
    public ScreenplayCompiler()
        : this(ScreenplayLanguageRegistry.Default)
    {
    }

    /// <inheritdoc/>
    public CompilationResult<ApplicationSyntax> Compile(string source)
    {
        var lines = SourceLineSplitter.Split(source);
        var context = new ParserContext(new(lines), languages: languages);
        var application = ScreenplayParser.Parse(context, lines);
        ScreenplayValidator.Validate(application, context);
        return new(application, context.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<TApplication> Compile<TApplication>(string source, IApplicationSyntaxVisitor<TApplication> visitor)
    {
        var result = Compile(source);
        return result.Success
            ? new(visitor.Visit(result.Value!), result.Diagnostics)
            : CompilationResult<TApplication>.Failed(result.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<ApplicationSyntax> Parse(string source, string? path = null)
    {
        var lines = SourceLineSplitter.Split(source, path: path);
        var context = new ParserContext(new(lines), path, languages);
        return new(ScreenplayParser.Parse(context, lines), context.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<ProjectionSyntax> CompileProjection(string source)
    {
        var lines = SourceLineSplitter.Split(source, hashComments: true);
        var context = new ParserContext(new(lines), languages: languages);
        var projections = ProjectionParser.ParseDocument(context);
        return new(projections.Count > 0 ? projections[0] : null, context.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<TProjection> CompileProjection<TProjection>(string source, IProjectionSyntaxVisitor<TProjection> visitor)
    {
        var result = CompileProjection(source);
        return result.Success
            ? new(visitor.Visit(result.Value!), result.Diagnostics)
            : CompilationResult<TProjection>.Failed(result.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<SpecificationSyntax> CompileSpecification(string source)
    {
        var lines = SourceLineSplitter.Split(source, hashComments: true);
        var context = new ParserContext(new(lines), languages: languages);
        var specifications = SpecificationParser.ParseDocument(context);
        return new(specifications.Count > 0 ? specifications[0] : null, context.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<TSpecification> CompileSpecification<TSpecification>(string source, ISpecificationSyntaxVisitor<TSpecification> visitor)
    {
        var result = CompileSpecification(source);
        return result.Success
            ? new(visitor.Visit(result.Value!), result.Diagnostics)
            : CompilationResult<TSpecification>.Failed(result.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<CaptureSyntax> CompileCapture(string source)
    {
        var lines = SourceLineSplitter.Split(source, hashComments: true);
        var context = new ParserContext(new(lines), languages: languages);
        var captures = CaptureParser.ParseDocument(context);
        return new(captures.Count > 0 ? captures[0] : null, context.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<TCapture> CompileCapture<TCapture>(string source, ICaptureSyntaxVisitor<TCapture> visitor)
    {
        var result = CompileCapture(source);
        return result.Success
            ? new(visitor.Visit(result.Value!), result.Diagnostics)
            : CompilationResult<TCapture>.Failed(result.Diagnostics);
    }
}
