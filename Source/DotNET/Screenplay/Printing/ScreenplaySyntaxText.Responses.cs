// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

internal static partial class ScreenplaySyntaxText
{
    // Preserve double semantics but expand scientific notation to the existing concrete-value grammar.
    internal static string ResponseValue(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax { Value: ExactNumber exact } => exact.CanonicalText,
        LiteralExpressionSyntax { Value: double number } => ResponseNumber(number),
        _ => ExpressionCore(expression)
    };

    static string ResponseNumber(double number)
    {
        var text = number.ToString("R", CultureInfo.InvariantCulture);
        var exponentAt = text.IndexOf('E');
        if (exponentAt < 0) return text;
        var negative = text[0] == '-';
        var mantissa = text[(negative ? 1 : 0)..exponentAt];
        var point = mantissa.IndexOf('.');
        var digits = mantissa.Replace(".", string.Empty, StringComparison.Ordinal);
        var decimalAt = (point < 0 ? mantissa.Length : point) + int.Parse(text[(exponentAt + 1)..], CultureInfo.InvariantCulture);
        var expanded = ExpandDecimal(digits, decimalAt);

        return negative ? "-" + expanded : expanded;
    }

    static string ExpandDecimal(string digits, int decimalAt)
    {
        if (decimalAt <= 0) return $"0.{new string('0', -decimalAt)}{digits}";
        if (decimalAt >= digits.Length) return digits + new string('0', decimalAt - digits.Length);

        return digits.Insert(decimalAt, ".");
    }
}
