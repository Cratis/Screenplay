// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxSchema;

public class when_describing_structural_members : Specification
{
    JsonElement _event;
    JsonElement _property;
    JsonElement _capture;
    JsonElement _typeReference;

    void Because()
    {
        _event = SyntaxSchema.For("EventSyntax");
        _property = SyntaxSchema.For("PropertySyntax");
        _capture = SyntaxSchema.For("CaptureWhenSyntax");
        _typeReference = SyntaxSchema.For("TypeRefSyntax");
    }

    [Fact] void should_require_names() => _event.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ShouldContain("name");
    [Fact] void should_require_child_types() => _property.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ShouldContain("type");
    [Fact] void should_require_nonoptional_booleans() => _typeReference.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ShouldContain("isCollection");
    [Fact] void should_default_collections() => _event.GetProperty("properties").GetProperty("properties").GetProperty("default").GetArrayLength().ShouldEqual(0);
    [Fact] void should_exclude_source_locations() => _event.GetProperty("properties").TryGetProperty("location", out _).ShouldBeFalse();
    [Fact] void should_describe_init_members() => _event.GetProperty("properties").TryGetProperty("file", out _).ShouldBeTrue();
    [Fact] void should_describe_structural_kind_as_an_enum() => _capture.GetProperty("properties").GetProperty("syntaxKind").GetProperty("enum").EnumerateArray().Select(value => value.GetString()).ShouldContain("LogicalAnd");
    [Fact] void should_constrain_child_node_families() => _property.GetProperty("properties").GetProperty("type").GetProperty("oneOf")[0].GetProperty("$ref").GetString().ShouldEqual("#/$defs/TypeRefSyntax");
}
