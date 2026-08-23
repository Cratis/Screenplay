// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Defines the runtime-independent canonical JSON policy shared by semantic documents.
/// </summary>
static class CanonicalJson
{
    /// <summary>
    /// The maximum JSON container depth admitted both while writing and while reading.
    /// </summary>
    internal const int MaximumDepth = 64;

    /// <summary>
    /// Gets the strict canonical writer options.
    /// </summary>
    /// <remarks>
    /// Semantic strings never pass through the runtime JSON encoder. <see cref="WriteString"/> emits their fixed ASCII
    /// escaping directly, while every remaining string written by the serializers is an ASCII schema token or identity.
    /// </remarks>
    internal static JsonWriterOptions WriterOptions => new()
    {
        Indented = false,
        MaxDepth = MaximumDepth,
        SkipValidation = false
    };

    /// <summary>
    /// Gets the strict canonical reader options.
    /// </summary>
    internal static JsonReaderOptions ReaderOptions => new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = MaximumDepth
    };

    /// <summary>
    /// Writes Unicode NFC text as a JSON property value using fixed ASCII escaping.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The Unicode NFC value.</param>
    internal static void WriteString(Utf8JsonWriter writer, string name, string value)
    {
        writer.WritePropertyName(name);
        WriteStringValue(writer, value, name);
    }

    /// <summary>
    /// Writes Unicode NFC text as a JSON array value using fixed ASCII escaping.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The Unicode NFC value.</param>
    internal static void WriteStringValue(Utf8JsonWriter writer, string value) => WriteStringValue(writer, value, "string value");

    /// <summary>
    /// Requires text to be well-formed UTF-16 in Unicode Normalization Form C.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="name">The semantic field name.</param>
    /// <returns>The validated text.</returns>
    internal static string RequireNfc(string value, string name)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw new InvalidSemanticContract($"Canonical JSON field '{name}' must contain well-formed Unicode text.");
                }

                index++;
            }
            else if (char.IsLowSurrogate(value[index]))
            {
                throw new InvalidSemanticContract($"Canonical JSON field '{name}' must contain well-formed Unicode text.");
            }
        }

        if (!value.IsNormalized(NormalizationForm.FormC))
        {
            throw new InvalidSemanticContract($"Canonical JSON field '{name}' must use Unicode NFC text.");
        }

        return value;
    }

    /// <summary>
    /// Writes a canonical semantic decimal.
    /// </summary>
    /// <remarks>
    /// The invariant grammar is <c>0</c>, or an optional minus followed by a non-zero ASCII digit and zero or more
    /// ASCII digits, optionally followed by a period and one or more ASCII digits ending in a non-zero digit.
    /// Exponents, leading zeros, insignificant trailing fractional zeros, and negative zero are forbidden.
    /// </remarks>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The semantic decimal.</param>
    internal static void WriteDecimal(Utf8JsonWriter writer, string name, decimal value)
    {
        var bits = decimal.GetBits(value);
        var low = unchecked((uint)bits[0]);
        var middle = unchecked((uint)bits[1]);
        var high = unchecked((uint)bits[2]);
        var scale = (bits[3] >> 16) & 0xff;
        var negative = (bits[3] & int.MinValue) != 0 && (low != 0 || middle != 0 || high != 0);

        while (scale > 0)
        {
            var dividedLow = low;
            var dividedMiddle = middle;
            var dividedHigh = high;
            if (DivideByTen(ref dividedLow, ref dividedMiddle, ref dividedHigh) != 0)
            {
                break;
            }

            low = dividedLow;
            middle = dividedMiddle;
            high = dividedHigh;
            scale--;
        }

        Span<byte> digits = stackalloc byte[29];
        var digitStart = digits.Length;
        do
        {
            var remainder = DivideByTen(ref low, ref middle, ref high);
            digits[--digitStart] = (byte)('0' + remainder);
        }
        while (low != 0 || middle != 0 || high != 0);

        var digitCount = digits.Length - digitStart;
        Span<byte> canonical = stackalloc byte[64];
        var written = 0;
        if (negative)
        {
            canonical[written++] = (byte)'-';
        }

        if (scale == 0)
        {
            digits[digitStart..].CopyTo(canonical[written..]);
            written += digitCount;
        }
        else if (digitCount <= scale)
        {
            canonical[written++] = (byte)'0';
            canonical[written++] = (byte)'.';
            canonical.Slice(written, scale - digitCount).Fill((byte)'0');
            written += scale - digitCount;
            digits[digitStart..].CopyTo(canonical[written..]);
            written += digitCount;
        }
        else
        {
            var integerDigits = digitCount - scale;
            digits.Slice(digitStart, integerDigits).CopyTo(canonical[written..]);
            written += integerDigits;
            canonical[written++] = (byte)'.';
            digits[(digitStart + integerDigits)..].CopyTo(canonical[written..]);
            written += scale;
        }

        writer.WritePropertyName(name);
        writer.WriteRawValue(canonical[..written], skipInputValidation: true);
    }

    static void WriteStringValue(Utf8JsonWriter writer, string value, string name)
    {
        RequireNfc(value, name);
        var buffer = new ArrayBufferWriter<byte>(Math.Max(2, value.Length + 2));
        WriteByte(buffer, (byte)'"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"': WriteAscii(buffer, "\\\""); break;
                case '\\': WriteAscii(buffer, "\\\\"); break;
                case >= ' ' and <= '~': WriteByte(buffer, (byte)character); break;
                default:
                    WriteAscii(buffer, "\\u");
                    WriteHex(buffer, character);
                    break;
            }
        }

        WriteByte(buffer, (byte)'"');
        writer.WriteRawValue(buffer.WrittenSpan, skipInputValidation: true);
    }

    static void WriteHex(ArrayBufferWriter<byte> buffer, char value)
    {
        const string Hex = "0123456789ABCDEF";
        WriteByte(buffer, (byte)Hex[(value >> 12) & 0xf]);
        WriteByte(buffer, (byte)Hex[(value >> 8) & 0xf]);
        WriteByte(buffer, (byte)Hex[(value >> 4) & 0xf]);
        WriteByte(buffer, (byte)Hex[value & 0xf]);
    }

    static void WriteAscii(ArrayBufferWriter<byte> buffer, string value)
    {
        var destination = buffer.GetSpan(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            destination[index] = (byte)value[index];
        }

        buffer.Advance(value.Length);
    }

    static void WriteByte(ArrayBufferWriter<byte> buffer, byte value)
    {
        buffer.GetSpan(1)[0] = value;
        buffer.Advance(1);
    }

    static uint DivideByTen(ref uint low, ref uint middle, ref uint high)
    {
        ulong remainder = 0;
        var value = (remainder << 32) | high;
        high = (uint)(value / 10);
        remainder = value % 10;
        value = (remainder << 32) | middle;
        middle = (uint)(value / 10);
        remainder = value % 10;
        value = (remainder << 32) | low;
        low = (uint)(value / 10);
        return (uint)(value % 10);
    }
}
