// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Reads only already-admitted patterns; PortableMatchParser remains the dialect authority.
internal sealed class MatchAmbiguityParser(string pattern)
{
    int _offset;

    internal MatchAmbiguityNode Parse() => Expression();

    MatchAmbiguityNode Expression()
    {
        var start = _offset;
        var alternatives = new List<MatchAmbiguityNode> { Sequence() };
        while (Take('|')) alternatives.Add(Sequence());

        return new("alternate", start, _offset - start, [.. alternatives]);
    }

    MatchAmbiguityNode Sequence()
    {
        var start = _offset;
        var children = new List<MatchAmbiguityNode>();
        while (_offset < pattern.Length && pattern[_offset] is not (')' or '|')) children.Add(Quantified());

        return new("concat", start, _offset - start, [.. children]);
    }

    MatchAmbiguityNode Quantified()
    {
        var start = _offset;
        var atom = Atom();
        if (_offset == pattern.Length) return atom;
        var minimum = 0;
        int? maximum = null;
        if (Take('*')) { }
        else if (Take('+')) { minimum = 1; }
        else if (Take('?')) { maximum = 1; }
        else if (Take('{'))
        {
            minimum = Number();
            maximum = minimum;
            if (Take(',')) maximum = pattern[_offset] == '}' ? null : Number();
            Take('}');
        }
        else { return atom; }
        Take('?');

        return new("repeat", start, _offset - start, [atom], minimum, maximum);
    }

    MatchAmbiguityNode Atom()
    {
        var start = _offset;
        var character = pattern[_offset++];
        if (character == '(')
        {
            if (Take('?') && !Take(':'))
            {
                while (pattern[_offset++] != '>') { }
            }
            var body = Expression();
            Take(')');

            return new("group", start, _offset - start, [body]);
        }
        if (character == '[')
        {
            while (pattern[_offset] != ']')
            {
                if (pattern[_offset++] == '\\') _offset++;
            }
            _offset++;
        }
        else if (character == '\\')
        {
            var escape = pattern[_offset++];
            _offset += escape switch { 'x' => 2, 'u' => 4, _ => 0 };
        }
        if (character is '^' or '$') return new(character == '^' ? "start" : "end", start, 1, []);
        var parsed = new PortableMatchParser(pattern[start.._offset]).Parse();

        return new("characters", start, _offset - start, [], Characters: ((MatchCharacterExpression)parsed.Expression!).Characters);
    }

    int Number()
    {
        var value = 0;
        while (_offset < pattern.Length && char.IsAsciiDigit(pattern[_offset])) value = Math.Min(3001, (value * 10) + pattern[_offset++] - '0');

        return value;
    }

    bool Take(char character)
    {
        if (_offset == pattern.Length || pattern[_offset] != character) return false;
        _offset++;

        return true;
    }
}
