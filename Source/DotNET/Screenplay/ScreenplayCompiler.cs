// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
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
public class ScreenplayCompiler(IScreenplayLanguageRegistry languages) : IScreenplayCompiler, ICommandStreamCandidateParser
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
        var context = SourceOptionsParser.Create(lines, languages: languages, streamCandidates: CommandStreamCandidates.Capture([lines], languages));
        var application = SourceCommentCapture.Attach(ScreenplayParser.Parse(context, lines), lines) with { RegisteredTriggers = SnapshotTriggers(languages) };
        ScreenplayValidator.Validate(application, context);
        return new(application, [.. context.Diagnostics, .. ProductionDestinationDiagnostics.In(application)]);
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
    public CompilationResult<ApplicationSyntax> Parse(string source, string? path = null) =>
        ParsePlaced(source, path, PlayPlacement.Document, languages);

    /// <inheritdoc/>
    public CompilationResult<ApplicationSyntax> Parse(string source, string? path, PlayPlacement placement) =>
        ParsePlaced(source, path, placement, languages);

    /// <inheritdoc/>
    public CompilationResult<ProjectionSyntax> CompileProjection(string source)
    {
        var lines = SourceLineSplitter.Split(source, hashComments: true);
        var context = SourceOptionsParser.Create(lines, languages: languages, hashComments: true);
        var projections = ProjectionParser.ParseDocument(context);
        return new(projections.Count > 0 ? SourceCommentCapture.Attach(projections[0], lines, hashComments: true) : null, context.Diagnostics);
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
        var context = SourceOptionsParser.Create(lines, languages: languages, hashComments: true);
        var specifications = SpecificationParser.ParseDocument(context);
        return new(specifications.Count > 0 ? SourceCommentCapture.Attach(specifications[0], lines, hashComments: true) : null, context.Diagnostics);
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
        var context = SourceOptionsParser.Create(lines, languages: languages, hashComments: true);
        var captures = CaptureParser.ParseDocument(context);
        return new(captures.Count > 0 ? SourceCommentCapture.Attach(captures[0], lines, hashComments: true) : null, context.Diagnostics);
    }

    /// <inheritdoc/>
    public CompilationResult<TCapture> CompileCapture<TCapture>(string source, ICaptureSyntaxVisitor<TCapture> visitor)
    {
        var result = CompileCapture(source);
        return result.Success
            ? new(visitor.Visit(result.Value!), result.Diagnostics)
            : CompilationResult<TCapture>.Failed(result.Diagnostics);
    }

    CommandStreamCandidates ICommandStreamCandidateParser.CaptureCandidates(IEnumerable<(IReadOnlyList<SourceLine> Lines, PlayPlacement Placement)> documents) =>
        CommandStreamCandidates.Capture(documents, languages);

    CompilationResult<ApplicationSyntax> ICommandStreamCandidateParser.ParseWithCandidates(string source, string? path, PlayPlacement placement, CommandStreamCandidates candidates) =>
        ParseWithCandidates(source, path, placement, candidates);

    /// <summary>
    /// Parses source text in a placement with a given language registry.
    /// </summary>
    /// <param name="source">The source text to parse.</param>
    /// <param name="path">The path to attribute locations to.</param>
    /// <param name="placement">The <see cref="PlayPlacement"/> saying where the top level belongs.</param>
    /// <param name="languages">The <see cref="IScreenplayLanguageRegistry"/> saying what the compiler recognizes.</param>
    /// <returns>The <see cref="CompilationResult{TResult}"/> of the parse.</returns>
    internal static CompilationResult<ApplicationSyntax> ParsePlaced(string source, string? path, PlayPlacement placement, IScreenplayLanguageRegistry languages)
    {
        var lines = SourceLineSplitter.Split(source, path: path);
        return ParseWithCandidates(lines, path, placement, languages, CommandStreamCandidates.Capture([lines], languages, placement));
    }

    /// <summary>
    /// Finds the files a document imports and where in it each import is written.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="path">The path to attribute locations to.</param>
    /// <returns>Each import with the module and feature names around it, outermost first.</returns>
    internal static IReadOnlyList<DiscoveredFileImport> DiscoverImports(string source, string? path)
    {
        var lines = SourceLineSplitter.Split(source, path: path);
        return ScreenplayParser.DiscoverImports(SourceOptionsParser.Create(lines, path));
    }

    internal CompilationResult<ApplicationSyntax> ParseWithCandidates(string source, string? path, PlayPlacement placement, CommandStreamCandidates candidates) =>
        ParseWithCandidates(SourceLineSplitter.Split(source, path: path), path, placement, languages, candidates);

    static CompilationResult<ApplicationSyntax> ParseWithCandidates(IReadOnlyList<SourceLine> lines, string? path, PlayPlacement placement, IScreenplayLanguageRegistry languages, CommandStreamCandidates candidates)
    {
        var context = SourceOptionsParser.Create(lines, path, languages, streamCandidates: candidates);
        return new(SourceCommentCapture.Attach(ScreenplayParser.Parse(context, lines, placement), lines) with { RegisteredTriggers = SnapshotTriggers(languages) }, context.Diagnostics);
    }

    static ImmutableDictionary<string, TriggerDefinition> SnapshotTriggers(IScreenplayLanguageRegistry registry) =>
        registry.Triggers.ToImmutableDictionary(
            entry => entry.Key,
            entry => entry.Value with { Values = entry.Value.Values is { } values ? values.ToImmutableArray() : null },
            StringComparer.Ordinal);
}
