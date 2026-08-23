// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_unsupported_versions : a_valid_semantic_model
{
    Exception _languageException;
    Exception _semanticException;

    void Because()
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
        var language = json.Replace("\"languageVersion\":\"1.0\"", "\"languageVersion\":\"1.1\"", StringComparison.Ordinal);
        var semantic = json.Replace("\"semanticVersion\":\"1.0\"", "\"semanticVersion\":\"2.0\"", StringComparison.Ordinal);
        _languageException = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(language)));
        _semanticException = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(semantic)));
    }

    [Fact] void should_reject_unknown_language_minor() => _languageException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_unknown_semantic_major() => _semanticException.ShouldBeOfExactType<InvalidSemanticContract>();
}
