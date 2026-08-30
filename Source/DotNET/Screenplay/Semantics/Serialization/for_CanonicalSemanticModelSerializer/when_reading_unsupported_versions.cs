// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_unsupported_versions : a_valid_semantic_model
{
    Exception _languageMinorException;
    Exception _languageV2Exception;
    Exception _semanticV2Exception;

    void Because()
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
        var languageMinor = json.Replace("\"languageVersion\":\"1.0\"", "\"languageVersion\":\"1.1\"", StringComparison.Ordinal);
        var languageV2 = json.Replace("\"languageVersion\":\"1.0\"", "\"languageVersion\":\"2.0\"", StringComparison.Ordinal);
        var semanticV2 = json.Replace("\"semanticVersion\":\"1.0\"", "\"semanticVersion\":\"2.0\"", StringComparison.Ordinal);
        _languageMinorException = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(languageMinor)));
        _languageV2Exception = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(languageV2)));
        _semanticV2Exception = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(semanticV2)));
    }

    [Fact] void should_reject_unknown_language_minor() => _languageMinorException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_reserved_language_v2() => _languageV2Exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_reserved_semantic_v2() => _semanticV2Exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
