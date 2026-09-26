// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Shares numeric parsing and value comparisons between source syntax, typed JSON and consistency checks.
/// </summary>
internal static class NumericLiteral
{
    internal static object? Parse(string text)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var approximate) || !double.IsFinite(approximate))
        {
            return null;
        }

        // Faithful means the shortest round-trip Double spelling (R), interpreted as a base-10 number,
        // equals the authored base-10 value after removing insignificant zeros. This retains the exact
        // CLR type, printing, typed-JSON shape and Chronicle storage text of every faithful main literal.
        // In particular 0.1, 2.50, 1e-5 and 1e17 are faithful; comparing binary fractions to the
        // authored decimal would instead change virtually every existing fractional literal.
        if ((approximate == 0 && text[0] == '-') ||
            SameDecimalValue(text, approximate.ToString("R", CultureInfo.InvariantCulture)))
        {
            return approximate;
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var precise) &&
            IsExactDecimal(text, precise))
        {
            var normalized = precise.ToString("G29", CultureInfo.InvariantCulture);

            // G29 uses exponent notation for small values: NumberStyles.Number would throw here.
            precise = decimal.Parse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (precise == decimal.Truncate(precise) && precise >= long.MinValue && precise <= long.MaxValue)
            {
                return (long)precise;
            }

            return precise;
        }

        // Keep main's finite Double fallback when the authored value cannot be represented exactly
        // by Decimal (including values below Decimal's scale and values outside its range).
        return approximate;
    }

    internal static bool Equal(object? left, object? right)
    {
        if (Rational(left) is { } first && Rational(right) is { } second)
        {
            return first.Numerator * second.Denominator == second.Numerator * first.Denominator;
        }

        return Equals(left, right);
    }

    internal static bool CompatibleForAuthoring(object? left, object? right)
    {
        if (left?.GetType() == right?.GetType())
        {
            return Equal(left, right);
        }

        if (!Equal(left, right) && left is not double && right is not double)
        {
            return false;
        }

        // Cross-kind Double comparisons must preserve the decimal bound into ESM too: binary numeric
        // equality alone does not imply equal Convert.ToDecimal results at large magnitudes.
        double? floating = left is double first ? first : null;
        floating ??= right is double second ? second : null;
        var other = left is double ? right : left;
        if (floating is not { } number || !double.IsFinite(number))
        {
            return Equal(left, right);
        }

        try
        {
            return other switch
            {
                int integer => Convert.ToDecimal(number, CultureInfo.InvariantCulture) == integer && integer == number,
                long integer => Convert.ToDecimal(number, CultureInfo.InvariantCulture) == integer && integer == number,
                decimal precise => Convert.ToDecimal(number, CultureInfo.InvariantCulture) == precise && (double)precise == number,
                _ => false
            };
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    static bool SameDecimalValue(string left, string right)
    {
        var first = Normalize(left);
        var second = Normalize(right);
        return first is { } a && second is { } b && a == b;
    }

    static (string Digits, BigInteger Exponent)? Normalize(string text)
    {
        var marker = text.IndexOfAny(['e', 'E']);
        var mantissa = marker < 0 ? text : text[..marker];
        if (marker >= 0 && !BigInteger.TryParse(text[(marker + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        {
            return null;
        }

        var exponent = marker < 0 ? BigInteger.Zero : BigInteger.Parse(text[(marker + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var point = mantissa.IndexOf('.');
        if (point >= 0)
        {
            exponent -= mantissa.Length - point - 1;
            mantissa = mantissa.Remove(point, 1);
        }

        var negative = mantissa[0] == '-';
        var digits = (negative ? mantissa[1..] : mantissa).TrimStart('0');
        if (digits.Length == 0)
        {
            return ("0", BigInteger.Zero);
        }

        var trimmed = digits.TrimEnd('0');
        exponent += digits.Length - trimmed.Length;
        return (negative ? $"-{trimmed}" : trimmed, exponent);
    }

    static bool IsExactDecimal(string text, decimal value)
    {
        var bits = decimal.GetBits(value);
        var coefficient = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32) | (new BigInteger((uint)bits[2]) << 64);
        var represented = ((bits[3] & int.MinValue) != 0 ? "-" : string.Empty) + coefficient.ToString(CultureInfo.InvariantCulture);
        var scale = (bits[3] >> 16) & 0xff;
        return SameDecimalValue(text, scale == 0 ? represented : $"{represented}e-{scale}");
    }

    static (BigInteger Numerator, BigInteger Denominator)? Rational(object? value)
    {
        if (value is int integer)
        {
            return (integer, BigInteger.One);
        }

        if (value is long whole)
        {
            return (whole, BigInteger.One);
        }

        if (value is decimal precise)
        {
            var bits = decimal.GetBits(precise);
            var coefficient = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32) | (new BigInteger((uint)bits[2]) << 64);
            return ((bits[3] & int.MinValue) != 0 ? -coefficient : coefficient, BigInteger.Pow(10, (bits[3] >> 16) & 0xff));
        }

        if (value is float single)
        {
            return Rational((double)single);
        }

        if (value is not double number || !double.IsFinite(number))
        {
            return null;
        }

        var bits64 = BitConverter.DoubleToUInt64Bits(number);
        var exponentBits = (int)((bits64 >> 52) & 0x7ff);
        var significand = new BigInteger(bits64 & 0x000f_ffff_ffff_ffffUL);
        var exponent = exponentBits == 0 ? -1074 : exponentBits - 1075;
        if (exponentBits != 0)
        {
            significand += BigInteger.One << 52;
        }

        if ((bits64 >> 63) != 0)
        {
            significand = -significand;
        }

        return exponent >= 0 ? (significand << exponent, BigInteger.One) : (significand, BigInteger.One << -exponent);
    }
}
