// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_admitting_invalid_literal_values : Specification
{
    readonly string[] _values =
    [
        "[]",
        "{}",
        "1e9999",
        /*lang=json,strict*/
                             "{\"literalType\":\"System.IO.FileInfo\",\"value\":\"x\"}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Int32\",\"value\":\"2147483648\"}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Int32\",\"value\":\"1.5\"}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Decimal\",\"value\":\"0.00000000000000000000000000001\"}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Single\",\"value\":\"NaN\"}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Single\",\"value\":\"Infinity\"}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Int64\",\"value\":1}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Int64\",\"value\":\"1\",\"extra\":true}",
        /*lang=json,strict*/
                             "{\"literalType\":\"Int64\",\"value\":\"1\",\"value\":\"1\"}"
    ];
    Exception[] _errors;

    void Because() => _errors = [.. _values.Select(value => Catch.Exception(() =>
        SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>($"{{\"kind\":\"LiteralExpressionSyntax\",\"value\":{value}}}"))))];

    [Fact] void should_exercise_every_rejection() => _errors.Length.ShouldEqual(12);
    [Fact] void should_reject_every_invalid_literal() => _errors.All(error => error is InvalidSyntaxJson).ShouldBeTrue();
}
