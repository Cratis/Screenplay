// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Holds the state shared by the parsers - the line reader and the collected diagnostics.
/// </summary>
/// <param name="reader">The <see cref="LineReader"/> providing the source lines.</param>
/// <param name="path">The path of the file being parsed, or <c>null</c> when the source text has no file identity.</param>
/// <param name="languages">The <see cref="IScreenplayLanguageRegistry"/> saying what the compiler recognizes.</param>
internal sealed class ParserContext(LineReader reader, string? path = null, IScreenplayLanguageRegistry? languages = null)
{
    readonly List<Diagnostic> _diagnostics = [];

    /// <summary>
    /// Gets the immutable options established before reading document values.
    /// </summary>
    public SourceOptions SourceOptions { get; internal set; } = SourceOptions.Legacy;

    /// <summary>
    /// Gets the <see cref="LineReader"/> providing the source lines.
    /// </summary>
    public LineReader Reader => reader;

    /// <summary>Gets the immutable whole-input inventory used only to classify command stream headers.</summary>
    public CommandStreamCandidates? StreamCandidates { get; init; }

    /// <summary>
    /// Gets the <see cref="SourceLocation"/> of the start of the document being parsed.
    /// </summary>
    public SourceLocation Start { get; } = SourceLocation.Start.In(path);

    /// <summary>
    /// Gets the <see cref="IScreenplayLanguageRegistry"/> saying what this compilation recognizes.
    /// </summary>
    public IScreenplayLanguageRegistry Languages { get; } = languages ?? ScreenplayLanguageRegistry.Default;

    /// <summary>
    /// Gets the <see cref="Diagnostic">diagnostics</see> collected so far.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics
    {
        get
        {
            // Directive-shaped lines nobody claimed as a field are errors, wherever they sit.
            if (NestedNumericDirectives.Count > 0 && SourceOptions.NumericMode != NumericMode.Legacy && reader.AtEnd)
            {
                _diagnostics.AddRange(NestedNumericDirectives.OrderBy(_ => _.Key).Select(_ => _.Value));
                NestedNumericDirectives.Clear();
            }

            return _diagnostics;
        }
    }

    /// <summary>
    /// Gets the directive-shaped lines below the top level, keyed by line number, still unclaimed as a field.
    /// </summary>
    internal Dictionary<int, Diagnostic> NestedNumericDirectives { get; set; } = [];

    /// <summary>
    /// Creates a context for work that produces diagnostics without reading source lines, such as merging
    /// and validating the documents of a folder.
    /// </summary>
    /// <returns>A <see cref="ParserContext"/> with no lines to read.</returns>
    public static ParserContext ForDiagnostics() => new(new([]));

    /// <summary>
    /// Records that a line was read as a field, so it is not a misplaced numeric directive.
    /// </summary>
    /// <param name="line">The <see cref="SourceLine"/> read as a field.</param>
    public void ClaimField(SourceLine line) => NestedNumericDirectives.Remove(line.Number);

    /// <summary>
    /// Reports an error diagnostic.
    /// </summary>
    /// <param name="code">The <see cref="DiagnosticCodes">code</see> identifying the kind of diagnostic.</param>
    /// <param name="message">The message of the diagnostic.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the diagnostic.</param>
    public void Error(string code, string message, SourceLocation location) => _diagnostics.Add(Diagnostic.Error(code, message, location));

    /// <summary>
    /// Reports a warning diagnostic.
    /// </summary>
    /// <param name="code">The <see cref="DiagnosticCodes">code</see> identifying the kind of diagnostic.</param>
    /// <param name="message">The message of the diagnostic.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the diagnostic.</param>
    public void Warning(string code, string message, SourceLocation location) => _diagnostics.Add(Diagnostic.Warning(code, message, location));

    /// <summary>
    /// Adds a diagnostic that was produced outside the parsers.
    /// </summary>
    /// <param name="diagnostic">The <see cref="Diagnostic"/> to add.</param>
    public void Add(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

    /// <summary>
    /// Gets the next line belonging to the block of a parent line, without consuming it.
    /// </summary>
    /// <param name="parentIndent">The indentation of the line that opened the block.</param>
    /// <param name="line">The next <see cref="SourceLine"/> in the block when there is one.</param>
    /// <returns>Whether there is another line in the block.</returns>
    public bool TryPeekChild(int parentIndent, [NotNullWhen(true)] out SourceLine? line)
    {
        line = Reader.PeekSignificant();
        if (line is null || line.Indent <= parentIndent)
        {
            line = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Consumes every remaining line of a block - used to recover from an unparsable construct.
    /// </summary>
    /// <param name="parentIndent">The indentation of the line that opened the block.</param>
    public void SkipBlock(int parentIndent)
    {
        while (TryPeekChild(parentIndent, out _))
        {
            Reader.TakeSignificant();
        }
    }
}
