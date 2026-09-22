// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_admitting_wrong_structural_types : Specification
{
    readonly string[] _payloads =
    [
        /*lang=json,strict*/
                             "{\"kind\":12}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\"}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\",\"name\":null}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\",\"name\":12}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"properties\":null}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"properties\":{}}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"properties\":[null]}",
        /*lang=json,strict*/
        "{\"kind\":\"EventSyntax\",\"name\":\"x\",\"properties\":[{\"kind\":\"EventSyntax\",\"name\":\"bad\"}]}",
        /*lang=json,strict*/
        "{\"kind\":\"TagSyntax\",\"value\":{\"kind\":\"EventSyntax\",\"name\":\"bad\"}}",
        /*lang=json,strict*/
        "{\"kind\":\"TagSyntax\",\"value\":null}",
        /*lang=json,strict*/
        "{\"kind\":\"TypeRefSyntax\",\"name\":\"String\",\"isCollection\":\"false\",\"isOptional\":false}",
        /*lang=json,strict*/
        "{\"kind\":\"TypeRefSyntax\",\"name\":\"String\"}",
        /*lang=json,strict*/
        "{\"kind\":\"IntervalTriggerSourceSyntax\",\"amount\":1.5,\"unit\":\"Seconds\"}",
        /*lang=json,strict*/
        "{\"kind\":\"IntervalTriggerSourceSyntax\",\"amount\":2147483648,\"unit\":\"Seconds\"}",
        /*lang=json,strict*/
        "{\"kind\":\"IntervalTriggerSourceSyntax\",\"amount\":1,\"unit\":0}",
        /*lang=json,strict*/
        "{\"kind\":\"IntervalTriggerSourceSyntax\",\"amount\":1,\"unit\":\"0\"}",
        /*lang=json,strict*/
        "{\"kind\":\"IntervalTriggerSourceSyntax\",\"amount\":1,\"unit\":\"Unknown\"}",
        /*lang=json,strict*/
        "{\"kind\":\"ThemeSyntax\",\"name\":\"x\",\"compatibleWith\":[1]}",
        /*lang=json,strict*/
        "{\"kind\":\"ThemeSyntax\",\"name\":\"x\",\"compatibleWith\":[null]}",
        /*lang=json,strict*/
        "{\"kind\":\"ScheduleTriggerSourceSyntax\",\"time\":\"25:00:00\"}"
    ];
    Exception[] _errors;

    void Because() => _errors = [.. _payloads.Select(payload => Catch.Exception(() => SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(payload))))];

    [Fact] void should_check_every_payload() => _errors.Length.ShouldEqual(20);
    [Fact] void should_reject_every_type_mismatch() => _errors.All(error => error is InvalidSyntaxJson).ShouldBeTrue();
    [Fact] void should_name_the_invalid_property_path() => _errors.All(error => error.Message.Contains('$', StringComparison.Ordinal)).ShouldBeTrue();
}
