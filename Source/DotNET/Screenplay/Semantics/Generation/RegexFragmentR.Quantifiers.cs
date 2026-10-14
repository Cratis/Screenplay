// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed partial class RegexFragmentR
{
    RegexExpression Quantified(RegexExpression expression)
    {
        var minimum = 0;
        int? maximum;
        if (Take('?'))
        {
            maximum = 1;
        }
        else if (Take('*'))
        {
            maximum = null;
        }
        else if (Take('+'))
        {
            minimum = 1;
            maximum = null;
        }
        else if (Take('{'))
        {
            minimum = Number();
            maximum = minimum;
            if (Take(',')) maximum = _index < pattern.Length && pattern[_index] == '}' ? null : Number();
            if (!Take('}') || maximum < minimum) throw new OutsideRegexFragment("invalid repeat");
        }
        else
        {
            return expression;
        }

        Take('?'); // Lazy and greedy forms recognize the same language.
        if (minimum == 0 && maximum == 0) return RegexExpression.Empty;
        if (!boundToShape) return RegexExpression.Repeat(expression, minimum, maximum);
        if (expression.MinimumLength > 0 && (long)expression.MinimumLength * minimum > length) return RegexExpression.None;

        // No match in a length-L word consumes more than L nonempty repetitions. Nullable
        // repetitions can pad with epsilon, including to meet a larger authored minimum.
        var bound = expression.MinimumLength == 0 ? length : length / expression.MinimumLength;
        minimum = Math.Min(minimum, bound);
        if (maximum is not null) maximum = Math.Min(maximum.Value, bound);

        return RegexExpression.Repeat(expression, minimum, maximum);
    }

    int Number()
    {
        if (_index == pattern.Length || pattern[_index] is < '0' or > '9') throw new OutsideRegexFragment("invalid repeat");
        var value = 0;
        while (_index < pattern.Length && pattern[_index] is >= '0' and <= '9')
        {
            value = (int)Math.Min(boundToShape ? (long)length + 1 : GlushkovAutomaton.PositionBudget + 1, ((long)value * 10) + pattern[_index++] - '0');
        }

        return value;
    }
}
