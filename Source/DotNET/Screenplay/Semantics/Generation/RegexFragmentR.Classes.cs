// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed partial class RegexFragmentR
{
    string CharacterClass()
    {
        var negate = Take('^');
        if (_index == pattern.Length || pattern[_index] == ']') throw new OutsideRegexFragment("empty character class");
        var characters = new HashSet<char>();
        while (_index < pattern.Length && pattern[_index] != ']')
        {
            var start = ClassAtom();
            if (_index + 1 < pattern.Length && pattern[_index] == '-' && pattern[_index + 1] != ']')
            {
                _index++;
                var end = ClassAtom();
                if (start.Length != 1 || end.Length != 1 || start[0] > end[0]) throw new OutsideRegexFragment("character class range");
                characters.UnionWith(Enumerable.Range(start[0], end[0] - start[0] + 1).Select(value => (char)value));
            }
            else
            {
                characters.UnionWith(start);
            }
        }

        if (!Take(']')) throw new OutsideRegexFragment("unclosed character class");
        var projected = new string([.. characters.Where(character => character is >= ' ' and <= '~').Order()]);

        return negate ? RegexCharacters.Complement(projected) : projected;
    }

    string ClassAtom()
    {
        var character = Next();

        return character == '\\' ? RegexCharacters.Escape(Next(), true) : character.ToString();
    }
}
