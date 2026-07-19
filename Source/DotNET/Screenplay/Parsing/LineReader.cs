// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Provides sequential access to <see cref="SourceLine">source lines</see> for the parsers.
/// </summary>
/// <param name="lines">The lines to read.</param>
internal sealed class LineReader(IReadOnlyList<SourceLine> lines)
{
    int _index;

    /// <summary>
    /// Gets the next significant line without consuming it, skipping blank lines.
    /// </summary>
    /// <returns>The next significant <see cref="SourceLine"/>, or <c>null</c> at the end of input.</returns>
    public SourceLine? PeekSignificant()
    {
        while (_index < lines.Count && lines[_index].IsBlank)
        {
            _index++;
        }

        return _index < lines.Count ? lines[_index] : null;
    }

    /// <summary>
    /// Consumes and returns the next significant line, skipping blank lines.
    /// </summary>
    /// <returns>The consumed <see cref="SourceLine"/>.</returns>
    public SourceLine TakeSignificant()
    {
        var line = PeekSignificant()!;
        _index++;
        return line;
    }

    /// <summary>
    /// Consumes and returns the next line verbatim, including blank lines - used inside fenced code blocks.
    /// </summary>
    /// <returns>The consumed <see cref="SourceLine"/>, or <c>null</c> at the end of input.</returns>
    public SourceLine? TakeRaw() => _index < lines.Count ? lines[_index++] : null;
}
