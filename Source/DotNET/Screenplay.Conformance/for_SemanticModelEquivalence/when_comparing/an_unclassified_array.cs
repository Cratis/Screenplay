// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class an_unclassified_array : given.compiled_models
{
    byte[] _json;
    Exception _error;

    void Establish()
    {
        var canonical = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(Compile(Source)));
        _json = Encoding.UTF8.GetBytes($"{{\"unclassified\":[1,2],{canonical[1..]}");
    }

    void Because() => _error = Catch.Exception(() => SemanticModelNormalizer.NormalizeCanonicalJson(_json));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<SemanticNormalizationFailed>();
    [Fact] void should_name_the_path() => ((SemanticNormalizationFailed)_error).Path.ShouldEqual("/unclassified");
}
