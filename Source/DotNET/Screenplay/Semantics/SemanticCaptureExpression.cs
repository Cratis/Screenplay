// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Parses and evaluates the template expression a capture append can be guarded by:
/// <c>`status == "sent" &amp;&amp; overdue == true`</c>.
/// </summary>
/// <remarks>
/// The grammar is deliberately small, so every target evaluates it the same way: fields, string, number, boolean and
/// null literals, the comparisons <c>==</c>, <c>!=</c>, <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c> and <c>&gt;=</c>, and
/// <c>&amp;&amp;</c>, <c>||</c>, <c>!</c> and parentheses. A field standing alone is true when it holds the boolean
/// <c>true</c>. Ordering compares numbers only.
/// </remarks>
internal static class SemanticCaptureExpression
{
    /// <summary>
    /// Parses an expression, with or without its enclosing backticks.
    /// </summary>
    /// <param name="text">The expression text.</param>
    /// <param name="expression">The parsed expression.</param>
    /// <returns><c>true</c> when the text is an expression of the grammar.</returns>
    public static bool TryParse(string? text, out Node expression)
    {
        expression = new Literal(SemanticValue.Null);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var body = text.Trim();
        if (body.Length >= 2 && body[0] == '`' && body[^1] == '`')
        {
            body = body[1..^1];
        }

        var parser = new Parser(body);
        if (!parser.TryParse(out var parsed))
        {
            return false;
        }

        expression = parsed;
        return true;
    }

    /// <summary>
    /// Evaluates a parsed expression over an item's fields.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="field">Looks up a field value; <c>null</c> when the item has no such field.</param>
    /// <returns>Whether the expression holds.</returns>
    /// <exception cref="InvalidSemanticContract">The expression orders values that are not numbers.</exception>
    public static bool Evaluate(Node expression, Func<string, SemanticValue?> field) =>
        Evaluate(expression, name => new Lookup(field(name), null)).Holds;

    /// <summary>
    /// Evaluates an expression without treating an unsupported lookup as a missing field.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="field">Looks up a scalar, a missing field, or an explicitly unsupported source.</param>
    /// <returns>The boolean outcome or the first reached unsupported lookup.</returns>
    public static Evaluation Evaluate(Node expression, Func<string, Lookup> field)
    {
        switch (expression)
        {
            case Or or:
                var leftOr = Evaluate(or.Left, field);
                return leftOr.Unsupported is not null || leftOr.Holds ? leftOr : Evaluate(or.Right, field);
            case And and:
                var leftAnd = Evaluate(and.Left, field);
                return leftAnd.Unsupported is not null || !leftAnd.Holds ? leftAnd : Evaluate(and.Right, field);
            case Not not:
                var operand = Evaluate(not.Operand, field);
                return operand.Unsupported is not null ? operand : new(!operand.Holds, null);
            case Comparison comparison:
                return Compare(comparison, field);
            case Field single:
                var value = field(single.Name);
                return new(value.Value is SemanticBooleanValue { Value: true }, value.Unsupported);
            case Literal literal:
                return new(literal.Value is SemanticBooleanValue { Value: true }, null);
            default:
                throw new InvalidSemanticContract("A capture expression node is unknown.");
        }
    }

    static Evaluation Compare(Comparison comparison, Func<string, Lookup> field)
    {
        var firstLookup = Operand(comparison.Left, field);
        if (firstLookup.Unsupported is not null) return new(false, firstLookup.Unsupported);
        var secondLookup = Operand(comparison.Right, field);
        if (secondLookup.Unsupported is not null) return new(false, secondLookup.Unsupported);
        var left = firstLookup.Value ?? SemanticValue.Null;
        var right = secondLookup.Value ?? SemanticValue.Null;
        if (string.Equals(comparison.Operator, "==", StringComparison.Ordinal) || string.Equals(comparison.Operator, "!=", StringComparison.Ordinal))
        {
            var equal = left is SemanticNumberValue leftNumber && right is SemanticNumberValue rightNumber
                ? leftNumber.Value == rightNumber.Value
                : SemanticValueRules.AreEqual(left, right);
            return new(string.Equals(comparison.Operator, "==", StringComparison.Ordinal) ? equal : !equal, null);
        }

        if (left is not SemanticNumberValue first || right is not SemanticNumberValue second)
        {
            throw new InvalidSemanticContract($"The capture expression orders with '{comparison.Operator}' values that are not numbers.");
        }

        var holds = comparison.Operator switch
        {
            "<" => first.Value < second.Value,
            "<=" => first.Value <= second.Value,
            ">" => first.Value > second.Value,
            _ => first.Value >= second.Value
        };
        return new(holds, null);
    }

    static Lookup Operand(Node node, Func<string, Lookup> field) => node switch
    {
        Field named => field(named.Name),
        Literal literal => new(literal.Value, null),
        _ => throw new InvalidSemanticContract("A capture expression operand must be a field or a literal.")
    };

    /// <summary>
    /// A scalar or missing value, or a reached source the evaluator cannot read.
    /// </summary>
    /// <param name="Value">The scalar, or null for a missing field.</param>
    /// <param name="Unsupported">The unsupported source, never a missing value.</param>
    internal readonly record struct Lookup(SemanticValue? Value, string? Unsupported);

    /// <summary>
    /// A supported boolean outcome or a reached unsupported lookup.
    /// </summary>
    /// <param name="Holds">Whether a supported expression holds.</param>
    /// <param name="Unsupported">The reached unsupported source.</param>
    internal readonly record struct Evaluation(bool Holds, string? Unsupported);

    /// <summary>
    /// A node of a parsed capture expression.
    /// </summary>
    internal abstract record Node;

    /// <summary>
    /// Either side holds.
    /// </summary>
    /// <param name="Left">The left side.</param>
    /// <param name="Right">The right side.</param>
    internal sealed record Or(Node Left, Node Right) : Node;

    /// <summary>
    /// Both sides hold.
    /// </summary>
    /// <param name="Left">The left side.</param>
    /// <param name="Right">The right side.</param>
    internal sealed record And(Node Left, Node Right) : Node;

    /// <summary>
    /// The operand does not hold.
    /// </summary>
    /// <param name="Operand">The negated expression.</param>
    internal sealed record Not(Node Operand) : Node;

    /// <summary>
    /// A comparison of two operands.
    /// </summary>
    /// <param name="Left">The left operand.</param>
    /// <param name="Operator">The comparison operator.</param>
    /// <param name="Right">The right operand.</param>
    internal sealed record Comparison(Node Left, string Operator, Node Right) : Node;

    /// <summary>
    /// The value of a field of the item.
    /// </summary>
    /// <param name="Name">The field name.</param>
    internal sealed record Field(string Name) : Node;

    /// <summary>
    /// A literal value.
    /// </summary>
    /// <param name="Value">The value.</param>
    internal sealed record Literal(SemanticValue Value) : Node;

    sealed class Parser(string text)
    {
        static readonly string[] _operators = ["==", "!=", "<=", ">=", "<", ">"];
        int _position;
        int _depth;

        public bool TryParse(out Node expression)
        {
            expression = new Literal(SemanticValue.Null);
            var parsed = ParseOr();
            SkipWhitespace();
            if (parsed is null || _position != text.Length)
            {
                return false;
            }

            expression = parsed;
            return true;
        }

        Node? ParseOr()
        {
            var left = ParseAnd();
            while (left is not null && Take("||"))
            {
                var right = ParseAnd();
                left = right is null ? null : new Or(left, right);
            }

            return left;
        }

        Node? ParseAnd()
        {
            var left = ParseUnary();
            while (left is not null && Take("&&"))
            {
                var right = ParseUnary();
                left = right is null ? null : new And(left, right);
            }

            return left;
        }

        Node? ParseUnary()
        {
            if (++_depth > 32)
            {
                return null;
            }

            try
            {
                if (Peek("!") && !Peek("!="))
                {
                    _position = SkipWhitespace() + 1;
                    return ParseUnary() is { } operand ? new Not(operand) : null;
                }

                if (Take("("))
                {
                    var inner = ParseOr();
                    return inner is not null && Take(")") ? inner : null;
                }

                var left = ParseOperand();
                if (left is null)
                {
                    return null;
                }

                foreach (var candidate in _operators)
                {
                    if (Take(candidate))
                    {
                        return ParseOperand() is { } right ? new Comparison(left, candidate, right) : null;
                    }
                }

                return left;
            }
            finally
            {
                _depth--;
            }
        }

        Node? ParseOperand()
        {
            var start = SkipWhitespace();
            if (start >= text.Length)
            {
                return null;
            }

            if (text[start] == '"')
            {
                var end = text.IndexOf('"', start + 1);
                if (end < 0)
                {
                    return null;
                }

                _position = end + 1;
                return new Literal(SemanticValue.Text(text[(start + 1)..end]));
            }

            var position = start;
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '.' or '-' or '$'))
            {
                position++;
            }

            if (position == start)
            {
                return null;
            }

            _position = position;
            var word = text[start..position];
            if (string.Equals(word, "true", StringComparison.Ordinal) || string.Equals(word, "false", StringComparison.Ordinal))
            {
                return new Literal(SemanticValue.Boolean(string.Equals(word, "true", StringComparison.Ordinal)));
            }

            if (string.Equals(word, "null", StringComparison.Ordinal))
            {
                return new Literal(SemanticValue.Null);
            }

            if (char.IsDigit(word[0]) || word[0] == '-')
            {
                return decimal.TryParse(word, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
                    ? new Literal(SemanticValue.Number(number))
                    : null;
            }

            return word.StartsWith("$.", StringComparison.Ordinal) ? new Field(word[2..]) : new Field(word);
        }

        bool Peek(string token)
        {
            var start = SkipWhitespace();
            return string.CompareOrdinal(text, start, token, 0, token.Length) == 0;
        }

        bool Take(string token)
        {
            if (!Peek(token))
            {
                return false;
            }

            _position = SkipWhitespace() + token.Length;
            return true;
        }

        int SkipWhitespace()
        {
            while (_position < text.Length && char.IsWhiteSpace(text[_position]))
            {
                _position++;
            }

            return _position;
        }
    }
}
