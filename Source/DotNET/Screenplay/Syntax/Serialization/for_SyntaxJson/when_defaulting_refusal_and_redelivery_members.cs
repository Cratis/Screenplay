// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_defaulting_refusal_and_redelivery_members : Specification
{
    InvokesSyntax _invocation;
    SpecificationSyntax _specification;

    void Because()
    {
        using var invocation = JsonDocument.Parse("{\"kind\":\"InvokesSyntax\",\"command\":\"Claim\",\"mappings\":[]}");
        using var specification = JsonDocument.Parse("{\"kind\":\"SpecificationSyntax\",\"name\":\"Claiming\",\"thenEventsInAnyOrder\":false}");
        _invocation = (InvokesSyntax)SyntaxJson.Deserialize(invocation.RootElement);
        _specification = (SpecificationSyntax)SyntaxJson.Deserialize(specification.RootElement);
    }

    [Fact] void should_default_legacy_invocations_to_no_branches() => _invocation.OnRefused.ShouldBeEmpty();
    [Fact] void should_default_legacy_specifications_to_no_redelivery() => _specification.WhenRedelivered.ShouldBeNull();
    [Fact] void should_omit_empty_branches_from_legacy_bytes() => SyntaxJson.Serialize(_invocation).TryGetProperty("onRefused", out _).ShouldBeFalse();
    [Fact] void should_omit_absent_redelivery_from_legacy_bytes() => SyntaxJson.Serialize(_specification).TryGetProperty("whenRedelivered", out _).ShouldBeFalse();
}
