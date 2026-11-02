// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Syntax;

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
    readonly Dictionary<SyntaxNode, (int First, int Last)> _anchors = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ScreenTemplateSyntax, int> _fitsSlotAnchors = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SyntaxNode, Dictionary<int, int>> _directiveAnchors = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<SyntaxNode, Dictionary<string, List<(string Key, int Line)>>> _printedCollections = new(ReferenceEqualityComparer.Instance);
    int _depth;
    int _line;

    internal NumericMode NumericMode { get; init; }

    /// <summary>Gets the lines written for each syntax owner, keyed by reference identity.</summary>
    internal IReadOnlyDictionary<SyntaxNode, (int First, int Last)> Anchors => _anchors;

    /// <summary>Gets the printed fits-slot line for each template, keyed by reference identity.</summary>
    internal IReadOnlyDictionary<ScreenTemplateSyntax, int> FitsSlotAnchors => _fitsSlotAnchors;

    /// <summary>Gets printed lines by original line number for scalar directives of each owner.</summary>
    internal IReadOnlyDictionary<SyntaxNode, Dictionary<int, int>> DirectiveAnchors => _directiveAnchors;

    /// <summary>
    /// Prints a fragment in the owning document's numeric mode, without a preamble.
    /// </summary>
    /// <param name="expression">The expression to print.</param>
    /// <returns>The mode-checked fragment text.</returns>
    public string Expression(ExpressionSyntax expression) => ScreenplaySyntaxText.Expression(expression, NumericMode);

    /// <summary>
    /// Writes a line of text at the current indentation depth.
    /// </summary>
    /// <param name="text">The text to write; an empty string writes a bare blank line without indentation.</param>
    public void Line(string text)
    {
        if (text.Length == 0)
        {
            _builder.Append('\n');
            _line++;
            return;
        }

        for (var level = 0; level < _depth; level++)
        {
            _builder.Append(IndentUnit);
        }

        _builder.Append(text).Append('\n');
        _line++;
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
        _line++;
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
    /// Writes <c>&lt;target&gt; = &lt;value&gt;</c>, or - for an object or a list - the target followed by the value as an indented block.
    /// </summary>
    /// <param name="target">The text before the value, such as the property name.</param>
    /// <param name="value">The <see cref="ExpressionSyntax"/> to write.</param>
    /// <param name="node">The node the written lines belong to, or <c>null</c>.</param>
    /// <param name="scalar">Renders a scalar; defaults to <see cref="Expression(ExpressionSyntax)"/>.</param>
    internal void Assignment(string target, ExpressionSyntax value, SyntaxNode? node = null, Func<ExpressionSyntax, string>? scalar = null)
    {
        if (!ScreenplaySyntaxText.IsStructured(value))
        {
            var text = $"{target} = {(scalar ?? Expression)(value)}";
            if (node is null) Line(text);
            else Line(text, node);
            return;
        }

        if (node is null)
        {
            Structured(target, value, scalar);
            return;
        }

        using var anchor = Anchor(node);
        Structured(target, value, scalar);
    }

    /// <summary>
    /// Writes a header line followed by an object or a list as an indented block: <c>member = value</c> lines, one
    /// <c>item</c> line per list element, <c>&lt;header&gt; none</c> for an empty list and <c>&lt;header&gt; empty</c>
    /// for an object without members.
    /// </summary>
    /// <param name="header">The header line text, such as a property name.</param>
    /// <param name="value">The <see cref="ObjectExpressionSyntax"/> or <see cref="ListExpressionSyntax"/> to write.</param>
    /// <param name="scalar">Renders a scalar; defaults to <see cref="Expression(ExpressionSyntax)"/>.</param>
    internal void Structured(string header, ExpressionSyntax value, Func<ExpressionSyntax, string>? scalar = null) => WriteStructured(header, value, scalar ?? Expression);

    /// <summary>Tracks a declaration's first line and the end of its printed children.</summary>
    internal NodeScope Anchor(SyntaxNode node) => new(this, node, _line);

    /// <summary>Writes one line and associates it with its exact syntax node.</summary>
    internal void Line(string text, SyntaxNode node)
    {
        var first = _line;
        Line(text);
        _anchors[node] = (first, first);
    }

    /// <summary>Writes a scalar directive and associates its authored position with its owner.</summary>
    internal void DirectiveLine(string text, SyntaxNode owner, string key)
    {
        if (DirectiveLocationKeys.IsCollectionKey(key))
        {
            var kind = key[..key.IndexOf(':')];
            if (!_printedCollections.TryGetValue(owner, out var collections))
            {
                _printedCollections[owner] = collections = [];
            }

            if (!collections.TryGetValue(kind, out var entries))
            {
                collections[kind] = entries = [];
            }

            entries.Add((key, _line));
        }

        if (owner.DirectiveLocations.TryGetValue(key, out var location) && location.Line > 0)
        {
            if (!_directiveAnchors.TryGetValue(owner, out var lines))
            {
                _directiveAnchors[owner] = lines = [];
            }

            lines[location.Line] = _line;
            foreach (var displaced in owner.DirectiveLocations.Where(entry => entry.Key.StartsWith($"omitted:{key}:", StringComparison.Ordinal)))
            {
                lines[displaced.Value.Line] = _line;
            }
        }

        Line(text);
    }

    /// <summary>Matches in-place scalar collection edits to their authored comment anchors.</summary>
    internal void ResolveEditedCollectionAnchors()
    {
        foreach (var (owner, collections) in _printedCollections)
        {
            foreach (var (kind, printed) in collections)
            {
                var authored = owner.DirectiveLocations
                    .Where(entry => DirectiveLocationKeys.IsCollectionKey(entry.Key) &&
                        entry.Key.StartsWith(kind + ":", StringComparison.Ordinal))
                    .OrderBy(entry => entry.Value.Line)
                    .ToArray();
                if (authored.Length != printed.Count || authored.Any(entry => entry.Value.Line <= 0))
                {
                    continue;
                }

                var authoredValues = authored.Select(entry => entry.Key[..entry.Key.LastIndexOf(':')]).ToArray();
                var printedValues = printed.Select(entry => entry.Key[..entry.Key.LastIndexOf(':')]).ToArray();
                var unmatchedAuthored = Enumerable.Range(0, printed.Count)
                    .Where(index => authoredValues[index] != printedValues[index]).ToList();
                var unmatchedPrinted = unmatchedAuthored.ToList();
                var reordered = false;
                foreach (var printedIndex in unmatchedPrinted.ToArray())
                {
                    var match = unmatchedAuthored.FindIndex(index => authoredValues[index] == printedValues[printedIndex]);
                    if (match < 0)
                    {
                        continue;
                    }

                    // A value that survived at another index follows its value, not the old slot.
                    reordered = true;
                    unmatchedAuthored.RemoveAt(match);
                    unmatchedPrinted.Remove(printedIndex);
                }

                if (reordered || unmatchedAuthored.Count == 0 || !unmatchedAuthored.SequenceEqual(unmatchedPrinted))
                {
                    continue;
                }

                if (!_directiveAnchors.TryGetValue(owner, out var lines))
                {
                    _directiveAnchors[owner] = lines = [];
                }

                // Changed duplicates also renumber later occurrences of a value. Align
                // every authored slot so its existing anchor cannot attach to the wrong twin.
                for (var index = 0; index < printed.Count; index++)
                {
                    lines[authored[index].Value.Line] = printed[index].Line;
                }
            }
        }
    }

    /// <summary>Writes a template's fits-slot directive and records its line for source comments.</summary>
    internal void FitsSlotLine(string text, ScreenTemplateSyntax template)
    {
        _fitsSlotAnchors[template] = _line;
        Line(text);
    }

    void WriteStructured(string header, ExpressionSyntax value, Func<ExpressionSyntax, string> scalar)
    {
        if (value is ListExpressionSyntax list && !list.Items.Any())
        {
            Line($"{header} none");
            return;
        }

        if (value is ObjectExpressionSyntax obj && !obj.Members.Any())
        {
            Line($"{header} empty");
            return;
        }

        Line(header);
        using var indent = Indent();
        switch (value)
        {
            case ObjectExpressionSyntax members:
                foreach (var member in members.Members)
                {
                    var name = ScreenplaySyntaxText.MemberName(member.Name);
                    if (ScreenplaySyntaxText.IsStructured(member.Value)) WriteStructured(name, member.Value, scalar);
                    else Line($"{name} = {scalar(member.Value)}");
                }

                break;
            case ListExpressionSyntax items:
                foreach (var item in items.Items)
                {
                    if (ScreenplaySyntaxText.IsStructured(item)) WriteStructured("item", item, scalar);
                    else Line($"item {scalar(item)}");
                }

                break;
        }
    }

    /// <summary>Tracks a syntax owner's printed extent.</summary>
    internal readonly struct NodeScope(ScreenplayWriter writer, SyntaxNode node, int first) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            if (writer._line > first)
            {
                writer._anchors[node] = (first, writer._line - 1);
            }
        }
    }

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
