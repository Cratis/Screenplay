// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Holds the state shared by the parsers - the line reader and the collected diagnostics.
/// </summary>
/// <param name="reader">The <see cref="LineReader"/> providing the source lines.</param>
internal sealed class ParserContext(LineReader reader)
{
    readonly List<Diagnostic> _diagnostics = [];

    /// <summary>
    /// Gets the <see cref="LineReader"/> providing the source lines.
    /// </summary>
    public LineReader Reader => reader;

    /// <summary>
    /// Gets the <see cref="Diagnostic">diagnostics</see> collected so far.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>
    /// Reports an error diagnostic.
    /// </summary>
    /// <param name="message">The message of the diagnostic.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the diagnostic.</param>
    public void Error(string message, SourceLocation location) => _diagnostics.Add(Diagnostic.Error(message, location));

    /// <summary>
    /// Reports a warning diagnostic.
    /// </summary>
    /// <param name="message">The message of the diagnostic.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the diagnostic.</param>
    public void Warning(string message, SourceLocation location) => _diagnostics.Add(Diagnostic.Warning(message, location));

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
