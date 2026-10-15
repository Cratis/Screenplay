// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed class GenerationDateClasses(GenerationPattern pattern)
{
    readonly bool _fixedWidth = pattern.Tokens.All(token => token.Minimum == token.Maximum);
    readonly bool _fixedDigitPositions = pattern.Tokens.All(token => token.Segment is not null ||
        token.Characters.All(char.IsAsciiDigit) || token.Characters.All(character => !char.IsAsciiDigit(character)));

    internal string Key(DateOnly date, GenerationWorkBudget budget)
    {
        // A fixed-width product has disjoint positions: substituting date literals is a bijection.
        // With a checksum, digit positions must also be fixed. Mixed digit/nondigit classes can
        // change the weights of date digits, even at fixed character width; retain the full scan.
        if (!_fixedWidth || (pattern.Checksum is not null && !_fixedDigitPositions))
        {
            return string.Join('|', pattern.Tokens.Where(token => token.Segment is not null).Select(token => string.Concat(token.Expand(date))));
        }
        if (pattern.Checksum is null) return string.Empty;

        // Zero every non-date digit, retaining its position among ASCII digits. The checksum of
        // this mask is a bijective fingerprint of the date contribution: Luhn sum mod 10 with
        // right-hand parity, mod11 weighted sum with right-hand weights 2..7, or mod97 residue
        // with decimal place. Thus at most 10/11/97 classes; "10" names mod11's excluded digit.
        var mask = new StringBuilder();
        foreach (var token in pattern.Tokens)
        {
            var digits = token.Characters.All(char.IsAsciiDigit) ? "0" : string.Empty;
            if (token.Segment is not null) digits = string.Concat(token.Expand(date));
            for (var repetition = 0; repetition < token.Minimum; repetition++)
            {
                foreach (var digit in digits)
                {
                    budget.Spend(); // Inspect one fixed digit-position edge of the pattern.
                    mask.Append(digit);
                }
            }
        }
        budget.Spend(mask.Length); // One checksum-state transition per digit actually processed.

        return GenerationChecksum.Calculate(mask.ToString(), pattern.Checksum) ?? "10";
    }
}
