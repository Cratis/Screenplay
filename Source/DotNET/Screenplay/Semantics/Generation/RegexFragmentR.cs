// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed partial class RegexFragmentR(string pattern, int length)
{
    int _index;

    internal RegexExpression Parse()
    {
        if (pattern.Any(character => character > 0x7f)) throw new OutsideRegexFragment("non-ASCII");
        var expression = Alternatives(true);
        if (_index != pattern.Length) throw new OutsideRegexFragment("unmatched group");

        return expression;
    }

    RegexExpression Alternatives(bool top)
    {
        var expression = Sequence(top);
        while (Take('|')) expression = RegexExpression.Alternate(expression, Sequence(top));

        return expression;
    }

    RegexExpression Sequence(bool top)
    {
        var start = top && Take('^');
        var expression = RegexExpression.Empty;
        while (_index < pattern.Length && pattern[_index] is not ('|' or ')' or '$'))
        {
            expression = RegexExpression.Concat(expression, Quantified(Atom()));
        }

        var end = top && Take('$');
        if (end && _index < pattern.Length && pattern[_index] != '|') throw new OutsideRegexFragment("interior anchor");
        if (!top && _index < pattern.Length && pattern[_index] == '$') throw new OutsideRegexFragment("group anchor");
        if (top && !start) expression = RegexExpression.Concat(RegexExpression.Repeat(RegexExpression.Any, 0, null), expression);
        if (top && !end) expression = RegexExpression.Concat(expression, RegexExpression.Repeat(RegexExpression.Any, 0, null));

        return expression;
    }

    RegexExpression Atom()
    {
        var character = pattern[_index++];
        return character switch
        {
            '(' => Group(),
            '[' => RegexExpression.Character(CharacterClass()),
            '.' => RegexExpression.Any,
            '\\' => RegexExpression.Character(RegexCharacters.Escape(Next(), false)),
            '^' => throw new OutsideRegexFragment("interior anchor"),
            '{' or '}' or ']' => throw new OutsideRegexFragment($"unescaped {character}"),
            '?' or '*' or '+' => throw new OutsideRegexFragment("quantifier without atom"),
            _ => RegexExpression.Character(character.ToString())
        };
    }

    RegexExpression Group()
    {
        if (Take('?') && !Take(':'))
        {
            var character = _index < pattern.Length ? pattern[_index] : '\0';
            if (character is '=' or '!' || (character == '<' && _index + 1 < pattern.Length && pattern[_index + 1] is '=' or '!')) throw new OutsideRegexFragment("lookaround");
            if (character is '<' or '\'') throw new OutsideRegexFragment("named group");
            throw new OutsideRegexFragment("flags or group extension");
        }

        var expression = Alternatives(false);
        if (!Take(')')) throw new OutsideRegexFragment("unclosed group");

        return expression;
    }

    bool Take(char character)
    {
        if (_index == pattern.Length || pattern[_index] != character) return false;
        _index++;

        return true;
    }

    char Next()
    {
        if (_index == pattern.Length) throw new OutsideRegexFragment("dangling escape");

        return pattern[_index++];
    }
}
