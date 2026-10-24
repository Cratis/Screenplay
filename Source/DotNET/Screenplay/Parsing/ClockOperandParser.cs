// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses clock offsets and <c>date of</c> in conditions (decisions 0106/0107).
/// Bare <c>today</c> and <c>now</c> stay ordinary paths, preserving property names.
/// </summary>
/// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
/// <param name="tokens">The condition tokens.</param>
/// <param name="location">The <see cref="SourceLocation"/> of the condition.</param>
internal sealed class ClockOperandParser(ParserContext context, IReadOnlyList<string> tokens, SourceLocation location)
{
    static readonly string[] _units = ["day", "days", "hour", "hours", "minute", "minutes"];

    int _position;

    internal bool Malformed { get; private set; }

    bool HasMore => _position < tokens.Count;

    string Current => tokens[_position];

    bool NextIsSign => _position + 1 < tokens.Count && IsSign(tokens[_position + 1]);

    internal ExpressionSyntax? TryParse(ref int position)
    {
        _position = position;
        Malformed = false;
        var parsed = ParseClockOperand();
        if (parsed is not null || Malformed) position = _position;
        return parsed;
    }

    internal ExpressionSyntax? ParseRight(ref int position)
    {
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.ExpectedComparisonValue, "Expected a value to compare against", location);
            return null;
        }

        var right = TryParse(ref position);
        return Malformed ? null : right ?? ExpressionParser.ParseMappingSource(context, tokens[position++], location);
    }

    static bool IsSign(string token) => (token.StartsWith('+') || token.StartsWith('-')) && (token.Length == 1 || char.IsAsciiDigit(token[1]));

    static bool IsAmount(string text, out int amount)
    {
        amount = 0;
        return text.Length > 0 && text.All(char.IsAsciiDigit) && int.TryParse(text, out amount);
    }

    ExpressionSyntax? ParseClockOperand()
    {
        if (StartsDateOf()) return ParseDateOf();
        return StartsOffset() ? ParseOffset() : null;
    }

    bool StartsDateOf() => _position + 1 < tokens.Count && tokens[_position] == "date" && tokens[_position + 1] == "of";

    bool StartsOffset() => HasMore && (Current == "today" || Current == "now") && NextIsSign;

    string ReadAmountText(string sign)
    {
        if (sign.Length > 1) return sign[1..];
        return HasMore ? tokens[_position++] : string.Empty;
    }

    DateOfExpressionSyntax? ParseDateOf()
    {
        _position += 2;
        if (!HasMore)
        {
            Malformed = true;
            context.Error(DiagnosticCodes.ExpectedComparisonValue, "Expected an operand after 'date of'", location);
            return null;
        }

        var operand = StartsOffset() ? ParseOffset() : ExpressionParser.ParseMappingSource(context, tokens[_position++], location);
        return operand is null ? null : new DateOfExpressionSyntax(operand, location);
    }

    ClockOffsetExpressionSyntax? ParseOffset()
    {
        var clock = tokens[_position++];
        var sign = tokens[_position++];
        var amountText = ReadAmountText(sign);
        Malformed = true;
        if (!IsAmount(amountText, out var amount))
        {
            context.Error(DiagnosticCodes.UnexpectedTokenInCondition, $"Expected a whole number of days, hours or minutes after '{clock} {sign[0]}', got '{amountText}'", location);
            return null;
        }

        if (!HasMore || !_units.Contains(Current))
        {
            context.Error(DiagnosticCodes.UnexpectedTokenInCondition, $"Expected a unit (day, hour or minute) after '{clock} {sign[0]} {amountText}'", location);
            return null;
        }

        Malformed = false;
        return new ClockOffsetExpressionSyntax(clock, sign[0] == '-' ? -amount : amount, tokens[_position++], location);
    }
}
