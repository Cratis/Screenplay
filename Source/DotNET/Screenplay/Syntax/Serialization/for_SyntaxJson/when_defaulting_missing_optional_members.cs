// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_defaulting_missing_optional_members : Specification
{
    EventSyntax _event;
    EventSyntax _explicitNullTags;
    PropertySyntax _property;

    void Because()
    {
        _event = (EventSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>("{\"kind\":\"EventSyntax\",\"name\":\"Added\"}"));
        _explicitNullTags = (EventSyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>("{\"kind\":\"EventSyntax\",\"name\":\"Added\",\"tags\":null}"));
        _property = (PropertySyntax)SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>("""
            {"kind":"PropertySyntax","name":"title","type":{"kind":"TypeRefSyntax","name":"String","isCollection":false,"isOptional":false}}
            """));
    }

    [Fact] void should_default_required_collections_to_empty() => _event.Properties.ShouldBeEmpty();
    [Fact] void should_default_optional_collections_to_empty() => _event.Tags.ShouldBeEmpty();
    [Fact] void should_default_optional_children_to_null() => _event.File.ShouldBeNull();
    [Fact] void should_default_optional_scalars_from_the_constructor() => _property.IsIdentifier.ShouldBeFalse();
    [Fact] void should_treat_explicit_optional_null_collections_as_empty() => _explicitNullTags.Tags.ShouldBeEmpty();
    [Fact] void should_compare_the_equivalent_optional_collections_equally() => SyntaxJson.StructurallyEqual(_event, _explicitNullTags).ShouldBeTrue();
    [Fact] void should_create_nested_locations_on_the_server() => _property.Type.Location.ShouldEqual(Diagnostics.SourceLocation.Start);
}
