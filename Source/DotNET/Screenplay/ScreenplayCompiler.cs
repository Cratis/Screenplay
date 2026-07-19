// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayCompiler"/>.
/// </summary>
public class ScreenplayCompiler : IScreenplayCompiler
{
    /// <inheritdoc/>
    public CompilationResult<ApplicationSyntax> Compile(string source)
    {
        var lines = SourceLineSplitter.Split(source);
        var context = new ParserContext(new(lines));
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
    public CompilationResult<ProjectionSyntax> CompileProjection(string source)
    {
        var lines = SourceLineSplitter.Split(source, hashComments: true);
        var context = new ParserContext(new(lines));
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
}
