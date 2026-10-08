// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Formats portable stream identities without silently rewriting text or rounding integers.
/// </summary>
public static partial class SemanticStreamIdFormatter
{
    /// <summary>Formats text without rewriting its identity.</summary>
    /// <param name="value">The text.</param>
    /// <param name="formatted">The unchanged valid text, or null.</param>
    /// <param name="failure">The value-free failure reason.</param>
    /// <returns>Whether the text is nonempty, well-formed Unicode NFC.</returns>
    public static bool TryFormatText(string value, out string? formatted, out StreamIdFormatFailure failure)
    {
        formatted = null;
        failure = StreamIdFormatFailure.Empty;
        if (value.Length == 0) return false;
        for (var index = 0; index < value.Length; index++)
        {
            if (!char.IsSurrogate(value[index])) continue;
            if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
            }
            else
            {
                failure = StreamIdFormatFailure.LoneSurrogate;
                return false;
            }
        }
        if (!value.IsNormalized(NormalizationForm.FormC))
        {
            failure = StreamIdFormatFailure.NotNfc;
            return false;
        }
        formatted = value;
        failure = StreamIdFormatFailure.None;

        return true;
    }

    /// <summary>Accepts only authored N, D, B and P UUID forms.</summary>
    /// <param name="value">The authored UUID.</param>
    /// <param name="formatted">The lowercase D form, or null.</param>
    /// <param name="failure">The value-free failure reason.</param>
    /// <returns>Whether the value is a portable UUID.</returns>
    public static bool TryFormatUuidText(string value, out string? formatted, out StreamIdFormatFailure failure)
    {
        formatted = null;
        failure = StreamIdFormatFailure.MalformedUuid;
        if (!UuidRegex().IsMatch(value) || !Guid.TryParse(value, out var uuid)) return false;
        formatted = FormatUuid(uuid);
        failure = StreamIdFormatFailure.None;

        return true;
    }

    /// <summary>Formats a UUID as lowercase hyphenated text.</summary>
    /// <param name="value">The UUID.</param>
    /// <returns>The lowercase D form.</returns>
    public static string FormatUuid(Guid value) => value.ToString("D", CultureInfo.InvariantCulture);

    /// <summary>Formats an integer losslessly in invariant decimal.</summary>
    /// <param name="value">The integer.</param>
    /// <param name="doubleBound">Whether the Double numeric mode bound applies.</param>
    /// <param name="formatted">The canonical text, or null.</param>
    /// <param name="failure">The value-free failure reason.</param>
    /// <returns>Whether the integer is in the selected domain.</returns>
    public static bool TryFormatInteger(BigInteger value, bool doubleBound, out string? formatted, out StreamIdFormatFailure failure)
    {
        formatted = null;
        failure = StreamIdFormatFailure.OutOfRange;
        if (doubleBound && (value < -9007199254740991L || value > 9007199254740991L)) return false;
        formatted = value.ToString(CultureInfo.InvariantCulture);
        failure = StreamIdFormatFailure.None;

        return true;
    }

    /// <summary>Formats a Double-mode integer within the lossless bound.</summary>
    /// <param name="value">The numeric value.</param>
    /// <param name="formatted">The canonical text, or null.</param>
    /// <param name="failure">The value-free failure reason.</param>
    /// <returns>Whether the number is a finite, safe integer.</returns>
    public static bool TryFormatInteger(double value, out string? formatted, out StreamIdFormatFailure failure)
    {
        formatted = null;
        failure = StreamIdFormatFailure.NotIntegral;
        if (!double.IsFinite(value) || Math.Truncate(value) != value) return false;

        return TryFormatInteger(new BigInteger(value), true, out formatted, out failure);
    }

    /// <summary>Encodes already formatted parts in declaration order.</summary>
    /// <param name="parts">Canonical scalar parts.</param>
    /// <returns>The escaped composite identity.</returns>
    public static string EncodeComposite(IEnumerable<string> parts) => string.Join('|', parts.Select(part => part.Replace("%", "%25", StringComparison.Ordinal).Replace("|", "%7C", StringComparison.Ordinal)));

    /// <summary>Decodes once and checks canonical scalar spelling, never repairing an identity.</summary>
    /// <param name="value">The encoded identity.</param>
    /// <param name="kinds">The declared kinds, in declaration order.</param>
    /// <param name="parts">The decoded canonical parts, or null.</param>
    /// <param name="failure">The value-free failure reason.</param>
    /// <param name="doubleBound">Whether the Double numeric mode integer bound applies.</param>
    /// <returns>Whether the identity is canonical for this declaration.</returns>
    public static bool TryDecodeComposite(string value, IReadOnlyList<StreamIdScalarKind> kinds, out IReadOnlyList<string>? parts, out StreamIdFormatFailure failure, bool doubleBound = true)
    {
        parts = null;
        failure = StreamIdFormatFailure.Arity;
        var encoded = value.Split('|');
        if (kinds.Count < 2 || encoded.Length != kinds.Count) return false;
        var decodedParts = new List<string>();
        for (var index = 0; index < encoded.Length; index++)
        {
            var component = encoded[index];
            var decoded = new StringBuilder();
            for (var position = 0; position < component.Length; position++)
            {
                if (component[position] != '%')
                {
                    decoded.Append(component[position]);
                }
                else if (position + 2 < component.Length && ((component[position + 1] == '2' && component[position + 2] == '5') || (component[position + 1] == '7' && component[position + 2] == 'C')))
                {
                    decoded.Append(component[position + 1] == '2' ? '%' : '|');
                    position += 2;
                }
                else
                {
                    failure = StreamIdFormatFailure.Escape;
                    return false;
                }
            }
            var text = decoded.ToString();
            string? formatted = null;
            failure = StreamIdFormatFailure.Noncanonical;
            var valid = kinds[index] switch
            {
                StreamIdScalarKind.Text => TryFormatText(text, out formatted, out failure),
                StreamIdScalarKind.Uuid => TryFormatUuidText(text, out formatted, out failure),
                StreamIdScalarKind.WholeNumber => IntegerRegex().IsMatch(text) && BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer) && TryFormatInteger(integer, doubleBound, out formatted, out failure),
                _ => false
            };
            if (!valid) return false;
            if (formatted != text)
            {
                failure = StreamIdFormatFailure.Noncanonical;
                return false;
            }
            decodedParts.Add(text);
        }
        parts = decodedParts;
        failure = StreamIdFormatFailure.None;

        return true;
    }

    /// <summary>Provides a diagnostic message that never contains the value.</summary>
    /// <param name="failure">The failure reason.</param>
    /// <returns>The portable value-free message.</returns>
    public static string FailureMessage(StreamIdFormatFailure failure) => failure switch
    {
        StreamIdFormatFailure.Empty => "A stream id text literal cannot be empty.",
        StreamIdFormatFailure.NotNfc => "A stream id text literal must be Unicode NFC.",
        StreamIdFormatFailure.LoneSurrogate => "A stream id text literal cannot contain lone UTF-16 surrogates.",
        StreamIdFormatFailure.OutOfRange => "A stream id integer literal must be between -9007199254740991 and 9007199254740991 in Double numeric mode.",
        StreamIdFormatFailure.NotIntegral => "A stream id integer must be a finite integral value.",
        StreamIdFormatFailure.MalformedUuid => "A stream id UUID must use N, D, B or P form.",
        StreamIdFormatFailure.Arity => "A composite stream id must match the declared part count of at least two.",
        StreamIdFormatFailure.Escape => "A composite stream id permits only %25 and %7C escapes.",
        StreamIdFormatFailure.Noncanonical => "A composite stream id part must use canonical scalar spelling.",
        _ => string.Empty
    };

    [GeneratedRegex(@"^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}|\{[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\}|\([0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\))(?![\s\S])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex UuidRegex();

    [GeneratedRegex(@"^-?[0-9]+(?![\s\S])", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex IntegerRegex();
}
