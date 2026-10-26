// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Conformance.for_SemanticStringClassification;

public class when_discriminators_compete : Specification
{
    [Theory]
    [InlineData("claims", "type", """{"kind":"composite","type":"value"}""")]
    [InlineData("route", "source", """{"kind":"resolved","source":"value"}""")]
    [InlineData("children", "identifiedBy", """{"property":"identity","field":"literal","identifiedBy":"value"}""")]
    public void should_prefer_identity_to_a_literal_discriminator(string owner, string property, string json)
    {
        using var document = JsonDocument.Parse(json);
        SemanticStringClassification.Classify(document.RootElement, owner, property).ShouldEqual(SemanticStringKind.Identity);
    }

    [Theory]
    [InlineData("[1,null,\"text\"]")]
    [InlineData("[\"text\",false,{}]")]
    public void should_refuse_unknown_mixed_arrays_with_the_exact_context(string value)
    {
        using var document = JsonDocument.Parse($"{{\"unknown\":{value}}}");
        var error = Assert.Throws<SemanticNormalizationFailed>(() => SemanticStringClassification.Classify(document.RootElement, "container", "unknown"));
        error.Path.ShouldEqual("container/unknown");
        error.Message.ShouldEqual("Cannot normalise 'container/unknown': the canonical string field has no schema classification");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"unknown":null}""")]
    [InlineData("""{"unknown":42}""")]
    [InlineData("""{"unknown":false}""")]
    [InlineData("""{"unknown":[]}""")]
    [InlineData("""{"unknown":[1,null,false,{"text":"nested"},["nested"]]}""")]
    [InlineData("""{"unknown":{"text":"nested"}}""")]
    public void should_accept_absent_or_nonstring_fields_without_recursing(string json)
    {
        using var document = JsonDocument.Parse(json);
        SemanticStringClassification.Classify(document.RootElement, "container", "unknown").ShouldEqual(SemanticStringKind.Literal);
    }
}
