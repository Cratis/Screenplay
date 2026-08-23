// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_golden_model : Specification
{
    const string Expected = "{\"schema\":\"cratis.screenplay.esm\",\"schemaVersion\":1,\"languageVersion\":\"1.0\",\"semanticVersion\":\"1.0\",\"revision\":\"rev1:ba7253e0690ec4a4f9f4b8a5a0bf6d5f2e60debb5e13dde2beeee04cde00bd7d\",\"application\":{\"id\":\"sem1:0000000000000000000000000000000000000000000000000000000000000000\",\"name\":\"Golden\",\"concepts\":[],\"types\":[],\"modules\":[]}}";
    string _json;

    void Because()
    {
        var application = new SemanticApplication(
            SemanticId.Parse("sem1:0000000000000000000000000000000000000000000000000000000000000000"),
            "Golden",
            [],
            [],
            []);
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        _json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(model));
    }

    [Fact] void should_match_the_golden_json() => _json.ShouldEqual(Expected);
}
