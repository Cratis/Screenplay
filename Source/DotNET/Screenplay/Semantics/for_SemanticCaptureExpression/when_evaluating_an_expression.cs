// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticCaptureExpression;

public class when_evaluating_an_expression : Specification
{
    static readonly Dictionary<string, SemanticValue> _fields = new(StringComparer.Ordinal)
    {
        ["status"] = SemanticValue.Text("sent"),
        ["overdue"] = SemanticValue.Boolean(true),
        ["amount"] = SemanticValue.Number(120)
    };

    [Theory]
    [InlineData("`status == \"sent\" && overdue == true`", true)]
    [InlineData("`status != \"sent\" || overdue`", true)]
    [InlineData("`!(status == \"sent\")`", false)]
    [InlineData("`amount >= 120 && amount < 121`", true)]
    [InlineData("`$.amount > 120`", false)]
    [InlineData("`missing == null`", true)]
    [InlineData("`(status == \"paid\" || status == \"sent\") && !overdue`", false)]
    void should_evaluate_the_portable_grammar(string text, bool expected)
    {
        SemanticCaptureExpression.TryParse(text, out var expression).ShouldBeTrue();
        SemanticCaptureExpression.Evaluate(expression, field => _fields.GetValueOrDefault(field)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("`status ~ \"sent\"`")]
    [InlineData("`status == `")]
    [InlineData("`(status == \"sent\"`")]
    [InlineData("``")]
    void should_refuse_what_the_grammar_does_not_hold(string text) => SemanticCaptureExpression.TryParse(text, out _).ShouldBeFalse();

    [Fact] void should_refuse_to_order_text() =>
        Catch.Exception(() => SemanticCaptureExpression.Evaluate(Parse("`status > \"a\"`"), field => _fields.GetValueOrDefault(field))).ShouldBeOfExactType<InvalidSemanticContract>();

    static SemanticCaptureExpression.Node Parse(string text)
    {
        SemanticCaptureExpression.TryParse(text, out var expression);
        return expression;
    }
}
