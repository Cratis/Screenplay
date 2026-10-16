// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Decision 0084: this parser, not the host regex engine, decides dialect admission.
internal sealed partial class PortableMatchParser(string pattern)
{
    const int SizeLimit = 3000;
    readonly Dictionary<string, List<(int Scope, int Alternative)[]>> _names = new(StringComparer.Ordinal);
    readonly List<(int Scope, int Alternative)> _branches = [];
    int _scope;
    int _offset;
    MatchPatternResult? _failure;

    internal MatchPatternResult Parse()
    {
        if (pattern.Length > 1000) Refuse("pattern length", 1000);
        var expression = Expression();
        if (_failure is null && _offset != pattern.Length) Invalid("unmatched closing group", _offset);
        if (expression.Size > SizeLimit) Refuse("expanded pattern size", expression.BudgetOffset ?? _offset);

        return _failure ?? new(pattern, expression.Node, null, 0, false, null) { ExpandedSize = expression.Size };
    }

    static bool NameStart(char character) => char.IsAsciiLetter(character) || character == '_';

    Fragment Expression()
    {
        var alternatives = new List<MatchExpression>();
        var sequence = new List<MatchExpression>();
        var size = 0;
        int? budgetOffset = null;
        var scope = _scope++;
        _branches.Add((scope, 0));
        while (_failure is null && _offset < pattern.Length && pattern[_offset] != ')')
        {
            if (pattern[_offset] == '|')
            {
                alternatives.Add(MatchExpression.Sequence(sequence));
                sequence = [];
                _branches[^1] = (scope, _branches[^1].Alternative + 1);
                size = Math.Min(SizeLimit + 1, size + 1);
                if (size > SizeLimit) budgetOffset ??= _offset;
                _offset++;
                continue;
            }
            var start = _offset;
            var atom = Atom();
            atom = Quantify(atom);
            if (atom.Node != MatchExpression.Empty) sequence.Add(atom.Node);
            size = Math.Min(SizeLimit + 1, size + atom.Size);
            if (size > SizeLimit) budgetOffset ??= atom.BudgetOffset ?? start;
        }
        _branches.RemoveAt(_branches.Count - 1);

        alternatives.Add(MatchExpression.Sequence(sequence));

        return new(MatchExpression.Choice(alternatives), size, BudgetOffset: budgetOffset);
    }

    Fragment Atom()
    {
        var start = _offset;
        var character = pattern[_offset++];
        switch (character)
        {
            case '(': return Group(start);
            case '[': return new(new MatchCharacterExpression(CharacterClass(start)), 1);
            case '\\': return new(new MatchCharacterExpression(Escape(start, false)), 1);
            case '.': return new(new MatchCharacterExpression(MatchCharacterSet.Dot()), 1);
            case '^': return new(new MatchAssertionExpression(true), 1, false);
            case '$': return new(new MatchAssertionExpression(false), 1, false);
            case ']': Refuse("unescaped closing bracket", start); break;
            case '{' when IsCountedRepeat(start): Invalid("quantifier without an atom", start); break;
            case '{': case '}': Refuse("unescaped brace", start); break;
            case '*': case '+': case '?': Invalid("quantifier without an atom", start); break;
            default: return new(new MatchCharacterExpression(MatchCharacterSet.Character(character)), 1);
        }

        return new(MatchExpression.Empty, 0);
    }

    Fragment Group(int start)
    {
        if (Take('?'))
        {
            if (Take(':')) { }
            else if (At('=') || At('!') || (At('<') && _offset + 1 < pattern.Length && pattern[_offset + 1] is '=' or '!'))
            {
                Refuse("lookaround", start);
            }
            else if (Take('<'))
            {
                var nameStart = _offset;
                while (_offset < pattern.Length && pattern[_offset] != '>') _offset++;
                var name = pattern[nameStart.._offset];
                if (!Take('>')) Invalid("unterminated group name", nameStart);
                else if (!PortableMatchGroupName.IsJavaScriptName(name)) Invalid("group name", nameStart);
                else if (!NameStart(name[0]) || name.Skip(1).Any(character => !NameStart(character) && !char.IsAsciiDigit(character))) Refuse("group name", nameStart);
                else RegisterName(name, nameStart);
            }
            else
            {
                Refuse(At('i') || At('m') || At('s') || At('-') ? "inline options" : "group extension", start);
            }
        }
        var body = Expression();
        if (!Take(')')) Invalid("unterminated group", start);

        return body;
    }

    void RegisterName(string name, int start)
    {
        if (!_names.TryGetValue(name, out var occurrences)) _names[name] = occurrences = [];

        // Duplicate names are valid only when an intervening alternative makes participation disjoint.
        if (occurrences.Exists(previous => !previous.Any(branch => _branches.Exists(current => current.Scope == branch.Scope && current.Alternative != branch.Alternative)))) Invalid("duplicate group name", start);
        occurrences.Add([.. _branches]);
    }

    bool At(char character) => _offset < pattern.Length && pattern[_offset] == character;

    bool Take(char character)
    {
        if (!At(character)) return false;
        _offset++;

        return true;
    }

    void Refuse(string construct, int offset) => _failure ??= new(pattern, null, construct, offset, false, PortableMatchSuggestions.For(construct));

    void Invalid(string construct, int offset) => _failure ??= new(pattern, null, construct, offset, true, null);

    readonly record struct Fragment(MatchExpression Node, int Size, bool Quantifiable = true, int? BudgetOffset = null);
}
