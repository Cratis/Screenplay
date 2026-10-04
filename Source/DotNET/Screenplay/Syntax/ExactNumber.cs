// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an immutable, normalized number in the exact Decimal literal domain.
/// </summary>
/// <remarks>
/// Literal ingestion does not round or underflow. This value does not introduce arbitrary-precision arithmetic
/// or opt a document into exact mode. Canonical text is invariant fixed-point text, with zero normalized.
/// </remarks>
public sealed record ExactNumber
{
    const string MaximumCoefficient = "79228162514264337593543950335";

    ExactNumber(decimal value, string canonicalText)
    {
        Value = value;
        CanonicalText = canonicalText;
    }

    /// <summary>
    /// Gets the exactly represented Decimal value. Integers beyond Int64 remain integers in this domain.
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// Gets the normalized invariant fixed-point spelling, without exponents or insignificant fractional zeros.
    /// </summary>
    public string CanonicalText { get; }

    /// <summary>
    /// Gets whether this mathematical value is integral, independently of Int64 representability.
    /// </summary>
    public bool IsIntegral => ExactMathFacts.IsIntegral(this);

    /// <summary>
    /// Tries to read a complete ASCII numeric token without rounding, floating point or exponent-sized allocation.
    /// </summary>
    /// <param name="text">The complete token, without surrounding whitespace.</param>
    /// <param name="number">The immutable value when the token is exactly representable.</param>
    /// <returns>Whether the token is syntactically valid and exactly representable.</returns>
    public static bool TryParse(string text, [NotNullWhen(true)] out ExactNumber? number)
    {
        number = null;
        if (!Scan(text, out var negative, out var integerStart, out var integerLength, out var fractionStart, out var fractionLength, out var exponent))
        {
            return false;
        }

        var digits = string.Concat(text.AsSpan(integerStart, integerLength), text.AsSpan(fractionStart, fractionLength));
        var first = 0;
        while (first < digits.Length && digits[first] == '0')
        {
            first++;
        }

        if (first == digits.Length)
        {
            number = new(0m, "0");
            return true;
        }

        var last = digits.Length;
        while (digits[last - 1] == '0')
        {
            last--;
        }

        var scale = fractionLength - exponent - (digits.Length - last);
        var significantLength = last - first;
        var appendedZeros = scale < 0 ? -scale : 0;
        if (scale > 28 || significantLength + appendedZeros > MaximumCoefficient.Length)
        {
            return false;
        }

        // Only the bounded coefficient is materialized. An authored exponent never determines allocation.
        var coefficient = digits[first..last] + new string('0', (int)appendedZeros);
        if (coefficient.Length == MaximumCoefficient.Length && string.CompareOrdinal(coefficient, MaximumCoefficient) > 0)
        {
            return false;
        }

        var normalizedScale = (byte)Math.Max(0, scale);
        var bits = decimal.GetBits(decimal.Parse(coefficient, CultureInfo.InvariantCulture));
        var value = new decimal(bits[0], bits[1], bits[2], negative, normalizedScale);
        var canonical = FixedPoint(coefficient, normalizedScale, negative);
        number = new(value, canonical);
        return true;
    }

    /// <summary>
    /// Determines whether the text is one complete ASCII numeric token, independently of representability.
    /// </summary>
    /// <param name="text">The complete token, without surrounding whitespace.</param>
    /// <returns>Whether the token matches the exact scalar grammar.</returns>
    public static bool IsToken(string text) => Scan(text, out _, out _, out _, out _, out _, out _);

    /// <inheritdoc/>
    public override string ToString() => CanonicalText;

    static bool Scan(string text, out bool negative, out int integerStart, out int integerLength, out int fractionStart, out int fractionLength, out long exponent)
    {
        negative = text.Length > 0 && text[0] == '-';
        integerStart = negative ? 1 : 0;
        var position = integerStart;
        Digits(text, ref position);
        integerLength = position - integerStart;
        fractionStart = position;
        fractionLength = 0;
        exponent = 0;
        if (integerLength == 0)
        {
            return false;
        }

        if (position < text.Length && text[position] == '.')
        {
            fractionStart = ++position;
            Digits(text, ref position);
            fractionLength = position - fractionStart;
            if (fractionLength == 0)
            {
                return false;
            }
        }

        if (position < text.Length && text[position] is 'e' or 'E')
        {
            position++;
            var exponentNegative = position < text.Length && text[position] == '-';
            if (position < text.Length && text[position] is '+' or '-')
            {
                position++;
            }

            var exponentStart = position;

            // No fractional or trailing-zero count can cancel an exponent beyond this input-relative bound.
            var limit = (long)text.Length + 29;
            while (position < text.Length && IsDigit(text[position]))
            {
                exponent = Math.Min(limit, (exponent * 10) + text[position++] - '0');
            }

            if (position == exponentStart)
            {
                return false;
            }

            if (exponentNegative)
            {
                exponent = -exponent;
            }
        }

        return position == text.Length;
    }

    static void Digits(string text, ref int position)
    {
        while (position < text.Length && IsDigit(text[position]))
        {
            position++;
        }
    }

    static bool IsDigit(char value) => value is >= '0' and <= '9';

    static string FixedPoint(string coefficient, byte scale, bool negative)
    {
        var text = new StringBuilder(60);
        if (negative)
        {
            text.Append('-');
        }

        if (scale == 0)
        {
            text.Append(coefficient);
        }
        else if (coefficient.Length > scale)
        {
            text.Append(coefficient.AsSpan(0, coefficient.Length - scale))
                .Append('.')
                .Append(coefficient.AsSpan(coefficient.Length - scale));
        }
        else
        {
            text.Append("0.")
                .Append('0', scale - coefficient.Length)
                .Append(coefficient);
        }

        return text.ToString();
    }
}
