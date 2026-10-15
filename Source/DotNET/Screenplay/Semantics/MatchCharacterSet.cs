// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Sorted disjoint intervals of UTF-16 code units, independent of host Unicode categories.
internal sealed class MatchCharacterSet
{
    List<(int First, int Last)> _ranges = [];

    internal IEnumerable<(int First, int Last)> Ranges => _ranges;

    internal char? Single => _ranges.Count == 1 && _ranges[0].First == _ranges[0].Last ? (char)_ranges[0].First : null;

    internal static MatchCharacterSet Character(char character) => Range(character, character);

    internal static MatchCharacterSet Range(char first, char last) => new() { _ranges = [(first, last)] };

    internal static MatchCharacterSet Escape(char escape)
    {
        var result = char.ToLowerInvariant(escape) switch
        {
            'd' => Range('0', '9'),
            'w' => Word(),
            's' => Whitespace(),
            _ => new MatchCharacterSet()
        };
        if (char.IsAsciiLetterUpper(escape)) result.Complement();

        return result;
    }

    internal static MatchCharacterSet Dot()
    {
        var result = Character('\n');
        result.Union(Character('\r'));
        result.Union(Character('\u2028'));
        result.Union(Character('\u2029'));
        result.Complement();

        return result;
    }

    internal void Union(MatchCharacterSet other)
    {
        var merged = new List<(int First, int Last)>();
        foreach (var range in _ranges.Concat(other._ranges).OrderBy(range => range.First))
        {
            if (merged.Count > 0 && range.First <= merged[^1].Last + 1)
            {
                merged[^1] = (merged[^1].First, Math.Max(merged[^1].Last, range.Last));
            }
            else
            {
                merged.Add(range);
            }
        }
        _ranges = merged;
    }

    internal void Complement()
    {
        var complement = new List<(int First, int Last)>();
        var next = 0;
        foreach (var range in _ranges)
        {
            if (next < range.First) complement.Add((next, range.First - 1));
            next = range.Last + 1;
        }
        if (next <= char.MaxValue) complement.Add((next, char.MaxValue));
        _ranges = complement;
    }

    internal bool Contains(char character)
    {
        var first = 0;
        var last = _ranges.Count - 1;
        while (first <= last)
        {
            var middle = first + ((last - first) / 2);
            var range = _ranges[middle];
            if (character < range.First) last = middle - 1;
            else if (character > range.Last) first = middle + 1;
            else return true;
        }

        return false;
    }

    static MatchCharacterSet Word()
    {
        var result = Range('A', 'Z');
        result.Union(Range('a', 'z'));
        result.Union(Range('0', '9'));
        result.Union(Character('_'));

        return result;
    }

    static MatchCharacterSet Whitespace()
    {
        var result = Range('\t', '\r');
        result.Union(Range('\u2000', '\u200a'));
        foreach (var character in " \u00a0\u1680\u2028\u2029\u202f\u205f\u3000\ufeff") result.Union(Character(character));

        return result;
    }
}
