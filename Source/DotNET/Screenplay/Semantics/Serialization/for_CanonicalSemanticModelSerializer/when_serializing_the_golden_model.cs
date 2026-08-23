// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_golden_model : Specification
{
    const string Expected = "{\"schema\":\"cratis.screenplay.esm\",\"schemaVersion\":1,\"languageVersion\":\"1.0\",\"semanticVersion\":\"1.0\",\"revision\":\"rev1:1ee6c396e6fe7134dca89a706619a1b51fcec5d2cce0996ae20fed5a40ed3c50\",\"application\":{\"id\":\"sem1:0000000000000000000000000000000000000000000000000000000000000000\",\"name\":\"Golden\",\"concepts\":[],\"types\":[],\"modules\":[]}}";
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
