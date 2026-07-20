// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Accumulates indentation-based Screenplay source text - the offside-rule counterpart to the parsers.
/// </summary>
/// <remarks>
/// Every level of nesting is two spaces, matching the sample convention and the strictly-greater-indent
/// child rule the parsers use. Blank lines are inserted for readability only and are insignificant to
/// the grammar, so they never affect a round trip through the compiler.
/// </remarks>
internal sealed class ScreenplayWriter
{
    const string IndentUnit = "  ";

    readonly StringBuilder _builder = new();
    int _depth;

    /// <summary>
    /// Writes a line of text at the current indentation depth.
    /// </summary>
    /// <param name="text">The text to write; an empty string writes a bare blank line without indentation.</param>
    public void Line(string text)
    {
        if (text.Length == 0)
        {
            _builder.Append('\n');
            return;
        }

        for (var level = 0; level < _depth; level++)
        {
            _builder.Append(IndentUnit);
        }

        _builder.Append(text).Append('\n');
    }

    /// <summary>
    /// Writes a single blank separator line, coalescing consecutive blanks and never emitting a leading one.
    /// </summary>
    public void Blank()
    {
        if (_builder.Length == 0)
        {
            return;
        }

        if (_builder.Length >= 2 && _builder[^1] == '\n' && _builder[^2] == '\n')
        {
            return;
        }

        _builder.Append('\n');
    }

    /// <summary>
    /// Increases the indentation depth for the lifetime of the returned scope.
    /// </summary>
    /// <returns>A <see cref="Scope"/> that restores the previous depth when disposed.</returns>
    public Scope Indent()
    {
        _depth++;
        return new Scope(this);
    }

    /// <inheritdoc/>
    public override string ToString() => _builder.ToString().TrimStart('\n').TrimEnd('\n') + '\n';

    /// <summary>
    /// Represents an indentation scope that restores the previous depth when disposed.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> the scope belongs to.</param>
    internal readonly struct Scope(ScreenplayWriter writer) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => writer._depth--;
    }
}
