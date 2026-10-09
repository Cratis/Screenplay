// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Serialization.for_SyntaxJson;

public class when_round_tripping_public_event_metadata
{
    [Theory]
    [InlineData("{\"kind\":\"EventSyntax\",\"name\":\"Shipped\"}")]
    [InlineData("{\"kind\":\"ImportSyntax\",\"qualifiedName\":\"Shipping.Shipped\"}")]
    void should_default_legacy_event_and_import_json_without_changing_serialized_bytes(string json)
    {
        var node = SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(json));
        var encoded = SyntaxJson.Serialize(node).GetRawText();
        encoded.ShouldNotContain("visibility");
        encoded.ShouldNotContain("origin");
        (node is EventSyntax @event ? @event.Visibility : ((ImportSyntax)node).Visibility).ShouldEqual(EventVisibility.Private);
    }

    [Theory]
    [InlineData("{\"kind\":\"EventSyntax\",\"name\":\"Shipped\",\"visibility\":\"Public\",\"origin\":\"../opaque.play\"}")]
    [InlineData("{\"kind\":\"ImportSyntax\",\"qualifiedName\":\"Shipping.Shipped\",\"visibility\":\"Public\",\"origin\":\"../opaque.play\"}")]
    void should_preserve_metadata_through_typed_json(string json)
    {
        var node = SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(json));
        var encoded = SyntaxJson.Serialize(node);
        encoded.GetProperty("visibility").GetString().ShouldEqual("Public");
        encoded.GetProperty("origin").GetString().ShouldEqual("../opaque.play");
        SyntaxJson.StructurallyEqual(node, SyntaxJson.Deserialize(encoded)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("{\"kind\":\"EventSyntax\",\"name\":\"Shipped\",\"visibility\":\"Unknown\"}")]
    [InlineData("{\"kind\":\"EventSyntax\",\"name\":\"Shipped\",\"origin\":\"store\"}")]
    [InlineData("{\"kind\":\"ImportSyntax\",\"qualifiedName\":\"Shipping.Shipped\",\"visibility\":\"Public\"}")]
    [InlineData("{\"kind\":\"ImportSyntax\",\"qualifiedName\":\"Shipping.Shipped\",\"visibility\":\"Public\",\"origin\":\"\"}")]
    void should_reject_unrepresentable_contract_metadata(string json) => Assert.Throws<InvalidSyntaxJson>(() => SyntaxJson.Deserialize(JsonSerializer.Deserialize<JsonElement>(json)));

    [Fact]
    void should_describe_optional_members_and_not_the_effective_direction()
    {
        var eventSchema = SyntaxSchema.For(nameof(EventSyntax));
        eventSchema.GetProperty("properties").TryGetProperty("visibility", out _).ShouldBeTrue();
        eventSchema.GetProperty("required").EnumerateArray().Any(value => value.GetString() == "visibility").ShouldBeFalse();
        SyntaxSchema.For(nameof(ImportSyntax)).GetProperty("properties").TryGetProperty("origin", out _).ShouldBeTrue();
        var sliceSchema = SyntaxSchema.For(nameof(SliceSyntax)).GetProperty("properties");
        sliceSchema.TryGetProperty("direction", out _).ShouldBeTrue();
        sliceSchema.TryGetProperty("effectiveDirection", out _).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_the_constructor_and_deconstruct_signatures()
    {
        typeof(EventSyntax).GetConstructor([typeof(string), typeof(IEnumerable<PropertySyntax>), typeof(SourceLocation), typeof(IEnumerable<TagSyntax>)]).ShouldNotBeNull();
        typeof(EventSyntax).GetMethods().Any(method => method.Name == "Deconstruct" && method.GetParameters().Length == 4).ShouldBeTrue();
        typeof(ImportSyntax).GetConstructor([typeof(string), typeof(SourceLocation)]).ShouldNotBeNull();
        typeof(ImportSyntax).GetMethods().Any(method => method.Name == "Deconstruct" && method.GetParameters().Length == 2).ShouldBeTrue();
        typeof(SliceSyntax).GetConstructors().Single().GetParameters().Length.ShouldEqual(15);
        typeof(SliceSyntax).GetMethods().Any(method => method.Name == "Deconstruct" && method.GetParameters().Length == 15).ShouldBeTrue();
    }
}
