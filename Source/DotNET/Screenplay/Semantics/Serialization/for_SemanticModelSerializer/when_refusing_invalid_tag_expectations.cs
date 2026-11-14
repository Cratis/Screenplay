// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_SemanticModelSerializer;

public class when_refusing_invalid_tag_expectations : a_plan_with_tag_expectations
{
    ExecutableSemanticModel _model;
    string _json;

    void Because()
    {
        _model = Model([["audit"]], [["audit"]]);
        _json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
    }

    [Fact] void should_refuse_tags_in_an_older_model() => Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(LanguageVersion.V53, SemanticVersion.V53, _model.Application));
    [Fact] void should_refuse_empty_tags_in_a_model() => Assert.Throws<InvalidSemanticContract>(() => Model([[]], [[""]]));
    [Fact] void should_refuse_whitespace_tags_in_a_model() => Assert.Throws<InvalidSemanticContract>(() => Model([[]], [[" "]]));

    [Fact]
    void should_refuse_tags_in_an_older_document()
    {
        var document = JsonNode.Parse(_json)!;
        document["schemaVersion"] = 53;
        document["languageVersion"] = LanguageVersion.V53.ToString();
        document["semanticVersion"] = SemanticVersion.V53.ToString();
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(document.ToJsonString())));
    }

    [Fact] void should_refuse_empty_items_in_a_document() => Refuse("[\"\"]");
    [Fact] void should_refuse_whitespace_items_in_a_document() => Refuse("[\" \" ]");
    [Fact] void should_refuse_non_string_items_in_a_document() => Refuse("[123]");
    [Fact] void should_refuse_null_items_in_a_document() => Refuse("[null]");

    void Refuse(string tags)
    {
        var document = _json.Replace("\"expectedTags\":[\"audit\"]", $"\"expectedTags\":{tags}", StringComparison.Ordinal);
        document.ShouldNotEqual(_json);
        Assert.Throws<InvalidSemanticContract>(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(document)));
    }
}
