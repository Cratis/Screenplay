// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics;

internal sealed partial class PortableMatchParser
{
    static int Bounded(BigInteger value) => (int)BigInteger.Min(SizeLimit + 1, value);

    Fragment Quantify(Fragment atom)
    {
        if (_failure is not null || _offset >= pattern.Length) return atom;
        var start = _offset;
        var count = BigInteger.One;
        var minimum = BigInteger.Zero;
        BigInteger? maximum = null;
        if (Take('*'))
        {
            // Zero or more, with one copy in the admission budget.
        }
        else if (Take('+'))
        {
            minimum = BigInteger.One;
        }
        else if (Take('?'))
        {
            maximum = BigInteger.One;
        }
        else if (Take('{'))
        {
            if (!Digits(out minimum))
            {
                Refuse("unescaped brace", start);
                return atom;
            }
            maximum = count = minimum;
            if (Take(','))
            {
                if (At('}'))
                {
                    maximum = null;
                    count = minimum + 1;
                }
                else if (!Digits(out count))
                {
                    Refuse("unescaped brace", start);
                    return atom;
                }
                else
                {
                    maximum = count;
                    if (count < minimum) Invalid("reversed counted repeat", start);
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
        var size = Bounded(atom.Size * count);
        int? budgetOffset = size > SizeLimit ? atom.BudgetOffset ?? start : null;

        // Zero-position bodies are empty. Huge counts are harmless only when a zero repeat erases them.
        var node = size == 0 ? MatchExpression.Empty : new MatchRepeatExpression(atom.Node, Bounded(minimum), maximum is { } upper ? Bounded(upper) : null);

        return new(node, size, false, budgetOffset);
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
