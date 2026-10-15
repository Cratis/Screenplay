// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics;

// Decision 0084: this parser, not the host regex engine, decides dialect admission.
internal sealed partial class PortableMatchParser(string pattern)
{
    const int SizeLimit = 1500;
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

        return _failure ?? new(pattern, expression.Text, null, 0, false, null);
    }

    static bool NameStart(char character) => char.IsAsciiLetter(character) || character == '_';

    Fragment Expression()
    {
        var text = new StringBuilder();
        var size = 0;
        int? budgetOffset = null;
        var scope = _scope++;
        _branches.Add((scope, 0));
        while (_failure is null && _offset < pattern.Length && pattern[_offset] != ')')
        {
            if (pattern[_offset] == '|')
            {
                text.Append('|');
                _branches[^1] = (scope, _branches[^1].Alternative + 1);
                _offset++;
                continue;
            }
            var start = _offset;
            var atom = Atom();
            atom = Quantify(atom);
            text.Append(atom.Text);
            size = Math.Min(SizeLimit + 1, size + atom.Size);
            if (size > SizeLimit) budgetOffset ??= atom.BudgetOffset ?? start;
        }
        _branches.RemoveAt(_branches.Count - 1);

        return new(text.ToString(), size, BudgetOffset: budgetOffset);
    }

    Fragment Atom()
    {
        var start = _offset;
        var character = pattern[_offset++];
        switch (character)
        {
            case '(': return Group(start);
            case '[': return new(CharacterClass(start).Emit(), 1);
            case '\\': return new(Escape(start, false).Emit(), 1);
            case '.': return new(MatchCharacterSet.Dot().Emit(), 1);
            case '^': return new(@"\A", 1, false);
            case '$': return new(@"\z", 1, false);
            case ']': Refuse("unescaped closing bracket", start); break;
            case '{': case '}': Refuse("unescaped brace", start); break;
            case '*': case '+': case '?': Invalid("quantifier without an atom", start); break;
            default: return new(MatchCharacterSet.Character(character).Emit(), 1);
        }

        return new(string.Empty, 0);
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
                if (name.Length == 0 || !NameStart(name[0]) || name.Skip(1).Any(character => !NameStart(character) && !char.IsAsciiDigit(character))) Refuse("group name", nameStart);
                else RegisterName(name, nameStart);
                if (!Take('>')) Invalid("unterminated group name", nameStart);
            }
            else
            {
                Refuse(At('i') || At('m') || At('s') || At('-') ? "inline options" : "group extension", start);
            }
        }
        var body = Expression();
        if (!Take(')')) Invalid("unterminated group", start);

        return new($"(?:{body.Text})", body.Size, BudgetOffset: body.BudgetOffset);
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

    readonly record struct Fragment(string Text, int Size, bool Quantifiable = true, int? BudgetOffset = null);
}
