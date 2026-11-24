// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_preserving_a_future_graph_projection_member : for_SemanticModelBinder.given.a_graph_projection_source
{
    ExecutableSemanticModel _model;
    byte[] _bytes;
    byte[] _rewritten;

    void Establish()
    {
        _model = Compile(Source);
        var json = JsonNode.Parse(SemanticModelSerializer.Serialize(_model));
        json["languageVersion"] = "58.1";
        json["semanticVersion"] = "58.1";
        json["application"]["graphProjections"][0]["futureScope"] = "kept";
        _bytes = for_UnknownPreservingSemanticModelReader.given.documents.Seal(json.AsObject());
    }

    void Because() => _rewritten = UnknownPreservingSemanticModelReader.Read(_bytes, new EsmAdmissionRange(58, 0, 1)).CopyBytes();

    [Fact] void should_keep_the_future_member() => Encoding.UTF8.GetString(_rewritten).ShouldContain("\"futureScope\":\"kept\"");
    [Fact] void should_keep_the_known_declaring_slice() => JsonNode.Parse(_rewritten)["application"]["graphProjections"][0]["declaringSlice"].GetValue<string>().ShouldEqual(_model.Application.GraphProjections.Single().DeclaringSlice!.Value.ToString());
}
