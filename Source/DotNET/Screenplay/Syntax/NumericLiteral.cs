// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Shares numeric parsing and exact value comparisons between source syntax, typed JSON and consistency checks.
/// </summary>
internal static class NumericLiteral
{
    internal static object? Parse(string text)
    {
        if (!text.Contains('.') && !text.Contains('e') && !text.Contains('E') &&
            long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return integer;
        }

        var mantissa = text.Split('e', 'E')[0];
        if (!mantissa.Any(digit => digit is >= '1' and <= '9'))
        {
            return decimal.Zero;
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var precise) &&
            IsExactDecimal(text, precise))
        {
            // Chronicle stores decimal syntax using Convert.ToString. Insignificant authored scale must not
            // make an otherwise unchanged projection definition look different on the next save.
            return decimal.Parse(precise.ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        // Main parsed every finite source number as Double. Keep the same fallback (and the same ESM
        // Convert.ToDecimal binding) for values Decimal cannot represent exactly, including underflow.
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var approximate) &&
            double.IsFinite(approximate)
                ? approximate
                : null;
    }

    internal static bool Equal(object? left, object? right)
    {
        if (Rational(left) is { } first && Rational(right) is { } second)
        {
            return first.Numerator * second.Denominator == second.Numerator * first.Denominator;
        }

        return Equals(left, right);
    }

    // Legacy in-memory Double edits used to print and reparse as Double. Admit their new Decimal spelling
    // only if both the binary Double round-trips and the bound ESM Decimal is unchanged.
    internal static bool CompatibleForAuthoring(object? left, object? right)
    {
        if (Equal(left, right))
        {
            return true;
        }

        double? floating = left is double first ? first : null;
        floating ??= right is double second ? second : null;
        decimal? precise = left is decimal firstDecimal ? firstDecimal : null;
        precise ??= right is decimal secondDecimal ? secondDecimal : null;
        if (floating is not { } number || precise is not { } value || !double.IsFinite(number))
        {
            return false;
        }

        if (number != 0 && value == 0)
        {
            return false;
        }

        try
        {
            return Convert.ToDecimal(number, CultureInfo.InvariantCulture) == value && (double)value == number;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    internal static string PrintDouble(double number, string shortest)
    {
        if (!double.IsFinite(number))
        {
            return shortest;
        }

        if (Parse(shortest) is { } parsed && CompatibleForAuthoring(number, parsed))
        {
            return shortest;
        }

        // The shortest round-trip Double can itself be an exactly representable Decimal, changing both
        // syntax kind and ESM bytes after printing. Emit the binary value's finite decimal expansion
        // instead. If even the binary value is an exact Decimal, add an insignificant tail to keep the
        // same Double on reparsing without changing its executable value.
        var bits = BitConverter.DoubleToUInt64Bits(number);
        var exponentBits = (int)((bits >> 52) & 0x7ff);
        var significand = new BigInteger(bits & 0x000f_ffff_ffff_ffffUL);
        var exponent = exponentBits == 0 ? -1074 : exponentBits - 1075;
        if (exponentBits != 0)
        {
            significand += BigInteger.One << 52;
        }

        if (significand.IsZero)
        {
            return shortest;
        }

        var negative = (bits >> 63) != 0 ? "-" : string.Empty;
        if (exponent >= 0)
        {
            return EnsureDouble(number, negative + (significand << exponent).ToString(CultureInfo.InvariantCulture));
        }

        var scale = -exponent;
        var digits = (significand * BigInteger.Pow(5, scale)).ToString(CultureInfo.InvariantCulture).PadLeft(scale + 1, '0');
        return EnsureDouble(number, $"{negative}{digits[..^scale]}.{digits[^scale..]}");
    }

    static string EnsureDouble(double number, string exact)
    {
        if (Parse(exact) is double)
        {
            return exact;
        }

        // Some binary Doubles ARE exact Decimals, but binding them through Convert.ToDecimal would round
        // away significant digits. A tiny decimal tail rounds back to the SAME Double while preventing
        // the source parser from switching to Decimal. Never silently print a different executable value.
        var padded = $"{exact}{(exact.Contains('.') ? string.Empty : ".")}{new string('0', 40)}1";
        if (Parse(padded) is double same && same == number)
        {
            return padded;
        }

        throw new UnsupportedSyntaxForPrinting("numeric literal", number.ToString("R", CultureInfo.InvariantCulture));
    }

    static bool IsExactDecimal(string text, decimal value)
    {
        var exponentStart = text.IndexOfAny(['e', 'E']);
        var mantissa = exponentStart < 0 ? text : text[..exponentStart];
        var exponent = 0;
        if (exponentStart >= 0 && !int.TryParse(text[(exponentStart + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
        {
            return false;
        }

        var decimalPoint = mantissa.IndexOf('.');
        var fractionalDigits = decimalPoint < 0 ? 0 : mantissa.Length - decimalPoint - 1;
        var coefficient = BigInteger.Parse(mantissa.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
        var bits = decimal.GetBits(value);
        var represented = new BigInteger((uint)bits[0]) | (new BigInteger((uint)bits[1]) << 32) | (new BigInteger((uint)bits[2]) << 64);
        if ((bits[3] & int.MinValue) != 0)
        {
            represented = -represented;
        }

        var shift = (long)exponent - fractionalDigits + ((bits[3] >> 16) & 0xff);

        // Remove insignificant zeros on BOTH sides before comparing: an arbitrary exponent window would
        // turn e.g. 9007199254740993 followed by 130 fractional zeros into an inexact Double.
        while (!coefficient.IsZero && coefficient % 10 == 0)
        {
            coefficient /= 10;
            shift++;
        }

        while (!represented.IsZero && represented % 10 == 0)
        {
            represented /= 10;
            shift--;
        }

        return shift == 0 && coefficient == represented;
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
