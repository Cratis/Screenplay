// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_admitting_unknown_or_duplicate_properties : Specification
{
    readonly string[] _payloads =
    [
        "{}",
        "null",
        "[]",
        /*lang=json,strict*/
                             "{\"kind\":\"System.IO.FileInfo\",\"name\":\"x\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"ExpressionSyntax\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"EventSyntax\",\"Kind\":\"EventSyntax\",\"name\":\"x\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"EventSyntax\",\"kind\":\"EventSyntax\",\"name\":\"x\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"name\":\"y\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"extra\":true}",
        /*lang=json,strict*/
                             "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"location\":{\"line\":8,\"column\":9}}",
        /*lang=json,strict*/
                             "{\"kind\":\"ImportSyntax\",\"qualifiedName\":\"A.B\",\"name\":\"B\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"ContextExpressionSyntax\",\"path\":\"tenant\",\"root\":\"tenant\"}",
        /*lang=json,strict*/
                             "{\"kind\":\"TagSyntax\",\"value\":{\"kind\":\"PathExpressionSyntax\",\"path\":\"a\",\"path\":\"b\"}}",
        /*lang=json,strict*/
                             "{\"kind\":\"TagSyntax\",\"value\":{\"kind\":\"PathExpressionSyntax\",\"path\":\"a\",\"extra\":0}}"
    ];
    Exception[] _errors;

    void Because() => _errors = [.. _payloads.Select(payload => Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(payload))))];

    [Fact] void should_check_every_payload() => _errors.Length.ShouldEqual(14);
    [Fact] void should_reject_every_payload_with_a_domain_error() => _errors.All(error => error is InvalidSyntaxJson).ShouldBeTrue();
    [Fact] void should_explain_every_rejection() => _errors.All(error => !string.IsNullOrWhiteSpace(error.Message)).ShouldBeTrue();
}
