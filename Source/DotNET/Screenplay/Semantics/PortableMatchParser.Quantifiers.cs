// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics;

internal sealed partial class PortableMatchParser
{
    Fragment Quantify(Fragment atom)
    {
        if (_failure is not null || _offset >= pattern.Length) return atom;
        var start = _offset;
        var count = BigInteger.One;
        if (At('*') || At('+') || At('?'))
        {
            _offset++;
        }
        else if (Take('{'))
        {
            if (!Digits(out var minimum))
            {
                Refuse("unescaped brace", start);
                return atom;
            }
            count = minimum;
            if (Take(','))
            {
                if (At('}'))
                {
                    count = minimum + 1;
                }
                else if (!Digits(out count))
                {
                    Refuse("unescaped brace", start);
                    return atom;
                }
                else if (count < minimum)
                {
                    Invalid("reversed counted repeat", start);
                }
            }
            if (!Take('}')) Refuse("unescaped brace", start);
        }
        else
        {
            return atom;
        }
        if (!atom.Quantifiable) Invalid("quantified assertion", start);
        Take('?');
        var size = (int)BigInteger.Min(SizeLimit + 1, atom.Size * count);
        int? budgetOffset = size > SizeLimit ? atom.BudgetOffset ?? start : null;

        // An empty group repeated any number of times still matches only the empty string.
        return new(size == 0 ? "(?:)" : atom.Text + pattern[start.._offset], size, false, budgetOffset);
    }

    bool Digits(out BigInteger value)
    {
        var start = _offset;
        value = BigInteger.Zero;
        while (_offset < pattern.Length && char.IsAsciiDigit(pattern[_offset]))
        {
            value = (value * 10) + (pattern[_offset++] - '0');
        }

        return _offset != start;
    }
}
