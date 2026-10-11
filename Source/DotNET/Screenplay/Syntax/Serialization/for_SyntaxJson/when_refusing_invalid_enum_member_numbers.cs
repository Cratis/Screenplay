// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_refusing_invalid_enum_member_numbers : Specification
{
    Exception?[] _errors;

    void Because() => _errors = [.. new[]
    {
        "\"type\":\"String\",\"values\":[\"draft\"],\"valueNumbers\":[1]",
        "\"type\":\"Enum\",\"values\":[\"draft\",\"issued\"],\"valueNumbers\":[1]",
        "\"type\":\"Enum\",\"values\":[\"draft\",\"issued\"],\"valueNumbers\":[1,1]",
        "\"type\":\"Enum\",\"values\":[\"draft\"],\"valueNumbers\":[2147483648]"
    }.Select(members => Catch.Exception(() => SyntaxJson.Deserialize(JsonDocument.Parse("{\"kind\":\"ConceptSyntax\",\"name\":\"Status\"," + members + "}").RootElement)))];

    [Fact] void should_reject_non_enums_cardinality_aliases_and_overflow() => _errors.All(error => error is InvalidSyntaxJson).ShouldBeTrue();
}
